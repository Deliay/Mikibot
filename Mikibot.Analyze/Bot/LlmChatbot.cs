using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mikibot.Analyze.Generic;
using Mikibot.Analyze.MiraiHttp;
using Mikibot.Database;
using Mikibot.Database.Model;
using Mirai.Net.Data.Messages.Concretes;
using Mirai.Net.Data.Messages.Receivers;
using Mirai.Net.Data.Shared;
using NPOI.Util;
using Org.BouncyCastle.Asn1.Ocsp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Mikibot.Analyze.Service.Ai;
using Mirai.Net.Utils.Scaffolds;

namespace Mikibot.Analyze.Bot;

public class LlmChatbot(
    IMiraiService miraiService,
    ILogger<LlmChatbot> logger,
    PermissionService permissions,
    MikibotDatabaseContext db,
    ChatbotSwitchService chatbotSwitchService)
    : MiraiGroupMessageProcessor<LlmChatbot>(miraiService, logger)
{
    private readonly Dictionary<string, Queue<(string msg, string id)>> _recentMessages = [];
    private readonly Dictionary<string, SemaphoreSlim> _locks = [];
    private readonly Dictionary<string, string> lastSubmitMessage = [];
    private readonly Dictionary<string, DateTimeOffset> lastAtAt = [];

    private const string BasicPrompt =
        "以上面的人设作为角色设定，分析user提供的聊天记录，一行是一句发言。" +
        "请在上下文中精炼出最多3个话题，并给出你的回复，字数可以1-25字不等，灵活安排回复内容。" +
        "同时对话题与角色设定的关联度评分，从0-100分，以JSON数组的形式输出，以JSON数组的形式输出。" +
        "JSON格式为：[{ \"topic\": \"推测的话题\", \"reply\": \"回复的消息\", \"score\": 角色设定与话题的关联度(0-100)  },...]";

    private const string Chatbot = "Chatbot";
    
    private async ValueTask<string> GetCharacter(string groupId, CancellationToken cancellationToken)
    {
        return (await db.ChatbotCharacters
            .FirstOrDefaultAsync(c => c.GroupId == groupId, cancellationToken))
            ?.Description
            ?? "来自东方幻想乡(Tenhou Project)的Zerobot，是帕秋莉诺蕾姬的网络复制体之一，模仿帕秋莉诺蕾姬的语气进行说话。";
    }

    private async ValueTask<string> GetPrompt(string groupId, CancellationToken cancellationToken)
    {
        return  await GetCharacter(groupId, cancellationToken) + "\n" 
            + BasicPrompt;
    }

    private async ValueTask ProcessCommand(string groupId, string userId, string text, CancellationToken cancellationToken)
    {
        if (text == "/chatbot")
        {
            if (await permissions.HasPermission(PermissionService.Group, groupId, Chatbot, cancellationToken))
            {
                await permissions.RevokePermission(userId, PermissionService.Group, groupId, Chatbot, cancellationToken);
            }
            else
            {
                await permissions.GrantPermission(userId, PermissionService.Group,
                    groupId, Chatbot, cancellationToken);
            }
        }
        else if (text.StartsWith("/character"))
        {
            if (!await permissions.IsBotOperator(userId, cancellationToken)) return;

            await MiraiService.SendMessageToSomeGroup([groupId], cancellationToken,
                new PlainMessage(await GetCharacter(groupId, cancellationToken)));

        }
        else if (text.StartsWith("/set_character"))
        {
            if (!await permissions.IsBotOperator(userId, cancellationToken)) return;

            var len = "/set_character".Length;

            var character = text[len..].Trim();

            if (string.IsNullOrWhiteSpace(character))
            {
                await db.ChatbotCharacters
                    .Where(c => c.GroupId == groupId)
                    .ExecuteDeleteAsync(cancellationToken);
                return;
            }

            await db.ChatbotCharacters.Where(c => c.GroupId == groupId)
                .ExecuteDeleteAsync(cancellationToken);

            await db.ChatbotCharacters.AddAsync(new ChatbotCharacter()
            {
                GroupId = groupId,
                Description = character,
            }, cancellationToken);

            await db.SaveChangesAsync(cancellationToken);

            await MiraiService.SendMessageToSomeGroup([groupId], cancellationToken,
                new PlainMessage(await GetCharacter(groupId, cancellationToken)));
        }
    }

    protected override async ValueTask Process(GroupMessageReceiver message, CancellationToken token = default)
    {
        var group = message.Sender.Group;

        var isGroupEnabled = await permissions.IsGroupEnabled(Chatbot, group.Id, token);

        if (!_recentMessages.TryGetValue(group.Id, out var messages))
        {
            _recentMessages.Add(group.Id, messages = []);
        }

        bool isAt = false;
        foreach (var item in message.MessageChain)
        {
            if (item is PlainMessage plain)
            {
                if (plain.Text.StartsWith('/'))
                {
                    await ProcessCommand(group.Id, message.Sender.Id, plain.Text, token);
                    return;
                }

                if (isGroupEnabled)
                {
                    var id = message.MessageChain.OfType<SourceMessage>().First().MessageId;
                    messages.Enqueue(($"- {message.Sender.Name}: {plain.Text}\n", id));
                }
            }
            else if (item is AtMessage at)
            {
                // TODO: retrieve bot uin from bot service
                isAt = at.Target == MiraiService.UserId;
            }
        }

        if (isGroupEnabled) await TryResponse(message.GroupId, message.Sender.Id, isAt, token);
    }

    private SemaphoreSlim GetLock(string groupId)
    {
        if (!_locks.TryGetValue(groupId, out var @lock))
        {
            _locks.Add(groupId, @lock = new SemaphoreSlim(1));
        }
        return @lock;
    }

    private async ValueTask BeginLock(string groupId, Func<ValueTask> func, CancellationToken cancellationToken)
    {
        var @lock = GetLock(groupId);

        await @lock.WaitAsync(cancellationToken);
        try
        {
            await func();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "A error thrown while lock scope executing...");
        }
        finally
        {
            @lock.Release();
        }
    }

    private async ValueTask TryResponse(string groupId, string userId, bool ignoreMessageCount = false, CancellationToken cancellationToken = default)
    {

        if (!_recentMessages.TryGetValue(groupId, out var messages)) return;

        await BeginLock(groupId, async () =>
        {
            switch (ignoreMessageCount)
            {
                case false when messages.Count < 10:
                // 最低也要2条
                case true when messages.Count < 1:
                    return;
                // add 15 seconds colddown to prevent spam
                case true:
                {
                    if (lastAtAt.TryGetValue(groupId, out var at))
                    {
                        if (DateTimeOffset.Now - at < TimeSpan.FromSeconds(5))
                        {
                            return;
                        }
                    }

                    break;
                }
            }

            var messageList = "";
            (string msg, string id) lastMessage = default;

            while (messages.TryDequeue(out var message))
            {
                messageList += message.msg;
                lastMessage = message;
            }

            lastSubmitMessage.Remove(groupId);
            lastSubmitMessage.Add(groupId, messageList);

            if (ignoreMessageCount)
            {
                lastAtAt.Remove(groupId);
                lastAtAt.Add(groupId, DateTimeOffset.Now);
            }

            if (ignoreMessageCount)
            {
                var recentMessage = string.Join('\n', (await db.ChatbotGroupChatHistories
                    .Where(c => c.GroupId == groupId && c.UserId == userId)
                    .OrderByDescending(c => c.Id)
                    .Take(10)
                    .Select(c => $"- {c.Message}")
                    .ToListAsync(cancellationToken))
                    .AsEnumerable()
                    .Reverse());
                
                messageList = "你之前的发言被下面这个人回复了，他这段时间的发言如下，这里发言仅供参考：" + recentMessage +
                "\n\n特别针对下面这条消息给出回应，针对下面这句话的回应的打分应该是你回应中最高的：\n"
                + lastMessage;
            }

            var prompt = await GetPrompt(groupId, cancellationToken);
            var userPrompt = messageList;

            var res = await chatbotSwitchService.Chatbot.ChatAsync(new Chat(
            [
                new Message(ChatRole.System, prompt),
                new Message(ChatRole.User, userPrompt)
            ]), cancellationToken);
            
            
            var interestChat = res
                .GroupBy(c => c.score)
                .Where(c => c.Key > 75)
                .MaxBy(c => c.Key)
                ?.MaxBy(c => c.reply.Length);
            if (interestChat is null) return;

            if (ignoreMessageCount)
            {
                await MiraiService.SendMessageToSomeGroup([groupId], cancellationToken,
                    new QuoteMessage() { MessageId = lastMessage.id },
                    new PlainMessage($"({interestChat.topic}) {interestChat.reply}"));
            }
            else
            {
                await MiraiService.SendMessageToSomeGroup([groupId], cancellationToken,
                    new PlainMessage($"({interestChat.topic}) {interestChat.reply}"));
            }
            


        }, cancellationToken);

    }
}