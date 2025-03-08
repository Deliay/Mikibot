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
using Manganese.Text;
using MathNet.Numerics;
using Microsoft.Extensions.AI;
using Mikibot.Analyze.Service.Ai;
using Mirai.Net.Data.Messages;
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

    private const string PromptPrefix = "你是一个聊天机器人，输出内容为一段JSON，你将扮演“----”里的角色：\n" +
                                        "----";
    
    private const string BasicPrompt =
        "----" +
        "\n以上面的人设作为角色设定，分析user提供的聊天记录，一行是一句发言。" +
        "请在上下文中精炼出最多3个话题，并给出你的回复。\n\n" +
        "如果你的判断需要画图，用你的想象力尽可能地描述图片，并用英文的描述填充在JSON中的imagePrompt字段里，" +
        "拒绝描述涉及色情或者人体模特、雕塑、露出全身等内容并将imagePrompt设置为空。" +
        "同时对话题与角色设定的关联度评分，从0-100分，以JSON数组的形式输出，以JSON数组的形式输出。如果有政治敏感内容，请用XX替代。" +
        "JSON前后不要携带任何内容。" +
        "消息中会携带消息id，请在话题中带上所关联的消息id。" +
        "JSON格式为：[{ \"imagePrompt\": \"你判断需要画图时这里是图片的prompt\" \"messageId\":\"消息id\", \"topic\": \"推测的话题\", \"reply\": \"回复的消息\", \"score\": 角色设定与话题的关联度(0-100)  },...]";

    private const string Chatbot = "Chatbot";

    private const string AnalysisPrompt = "你是一个人格分析机器人，输出格式是JSON，用户将会提供聊天记录，请在400字内总结出聊天记录中体现出的用户画像，" +
                                          "尽量引用聊天记录中的发言来进行陈述说明，让你的分析更有说服力，你的分析输出结果需要使用贴吧老哥和小红书集美混合的语气来输出。" +
                                          "同时你还要为其输出其人物的对应绘图二次元大头照关键词，其中带有anime。" +
                                          "你的输出JSON格式为：{\"character\":\"你分析的用户画像\", \"avatarPrompt\":\"对应人物的大头照片关键词\"}";
    private record AnalysisResult(string character, string avatarPrompt);
    
    private async ValueTask<string> GetCharacter(string groupId, CancellationToken cancellationToken)
    {
        return (await db.ChatbotCharacters  
            .FirstOrDefaultAsync(c => c.GroupId == groupId, cancellationToken))
            ?.Description
            ?? "来自东方幻想乡(Tenhou Project)的Zerobot，是帕秋莉诺蕾姬的网络复制体之一，模仿帕秋莉诺蕾姬的语气进行说话。";
    }

    private async ValueTask<string> GetPrompt(string groupId, CancellationToken cancellationToken)
    {
        return PromptPrefix + "\n" + await GetCharacter(groupId, cancellationToken) + "\n" 
            + BasicPrompt;
    }

    private static IEnumerable<MessageBase> ExpandAnalysis(AnalysisResult result)
    {
        if (result.avatarPrompt is { Length: > 0 })
        {
            var fileName = Guid.NewGuid().ToString();
            yield return new ImageMessage() { Url = $"https://image.pollinations.ai/prompt/{result.avatarPrompt}/{fileName}?width=1024&height=1024&seed=100&model=flux-anime&nologo=true&enhance=true" };
        }
        yield return new PlainMessage() { Text = result.character };
    }
    
    private async ValueTask ProcessCommand(string messageId, string groupId, string userId, string text, CancellationToken cancellationToken)
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
        else if (text.StartsWith("/我的群聊画像"))
        {
            var histories = await db.ChatbotGroupChatHistories
                .Where(c => c.GroupId == groupId && c.UserId == userId)
                .OrderByDescending(c => c.Id)
                .Take(300)
                .ToListAsync(cancellationToken);

            histories.Reverse();
            
            if (histories.Count < 20)
            {
                await MiraiService.SendMessageToSomeGroup([groupId], cancellationToken,
                    new PlainMessage($"需要至少50条消息才能生成画像~目前你已经发了{histories.Count}条"));
                return;
            }
            
            var chats = string.Join('\n', histories.Select(h => h.Message));
            var quote = new QuoteMessage() { MessageId = messageId };

            var result = await chatbotSwitchService.Chatbot
                .LlmChatAsync(new Chat(
                [
                    new Message(ChatRole.System, AnalysisPrompt),
                    new Message(ChatRole.User, chats)
                ]), cancellationToken);

            if (result is not { choices.Count: > 0 })
            {
                await MiraiService.SendMessageToSomeGroup([groupId], cancellationToken,
                    quote,
                    new PlainMessage("AI出错了"));
                return;
            }
            
            var messages = result.choices.Select(c => c.message
                    .TryPluckJsonObjectContent(out var jsonContent) ? jsonContent : null)
                .Where(c => c != null)
                .Select(c => JsonSerializer.Deserialize<AnalysisResult>(c!)!)
                .SelectMany(ExpandAnalysis)
                .Concat([quote])
                .ToArray();
            
            await MiraiService.SendMessageToSomeGroup([groupId], cancellationToken, messages);
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
        var id = message.MessageChain.OfType<SourceMessage>().First().MessageId;
        foreach (var item in message.MessageChain)
        {
            switch (item)
            {
                case PlainMessage plain when plain.Text.StartsWith('/'):
                    await ProcessCommand(id, group.Id, message.Sender.Id, plain.Text, token);
                    return;
                case PlainMessage plain when !isGroupEnabled:
                    continue;
                case PlainMessage plain:
                {
                    messages.Enqueue(($"- {message.Sender.Name}: {plain.Text}\n", id));
                    break;
                }
                case AtMessage at:
                    isAt = at.Target == MiraiService.UserId;
                    break;
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

    private async ValueTask TryResponse(string groupId, string userId, bool isBotHasBeenMentioned = false, CancellationToken cancellationToken = default)
    {

        if (!_recentMessages.TryGetValue(groupId, out var messages)) return;

        await BeginLock(groupId, async () =>
        {
            switch (isBotHasBeenMentioned)
            {
                case false when messages.Count < 20:
                    return;
                // add 5 seconds cold down to prevent spam
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
                messageList += $"[消息id:{message.id}] {message.msg}";
                lastMessage = message;
            }

            lastSubmitMessage.Remove(groupId);
            lastSubmitMessage.Add(groupId, messageList);

            if (isBotHasBeenMentioned)
            {
                lastAtAt.Remove(groupId);
                lastAtAt.Add(groupId, DateTimeOffset.Now);
            }

            if (isBotHasBeenMentioned)
            {
                var recentMessage = (await db.ChatbotGroupChatHistories
                    .Where(c => c.GroupId == groupId && c.UserId == userId)
                    .OrderByDescending(c => c.Id)
                    .Take(10)
                    .Select(c => $"-[消息ID: {c.MessageId}] 消息:{c.Message}")
                    .ToListAsync(cancellationToken))
                    .AsEnumerable()
                    .Reverse()
                    .JoinToString("\n");
                
                messageList = "你之前的发言被下面这个人回复了，他这段时间的发言如下，这里发言仅供参考：" + recentMessage +
                "\n\n特别针对下面这条消息给出回应，针对下面这句话的回应的打分应该是你回应中最高的：\n"
                + lastMessage;
            }

            var prompt = await GetPrompt(groupId, cancellationToken);
            var userPrompt = messageList;

            var userMessage = new Message(ChatRole.User, userPrompt);
            var chatbotContexts = Enumerable.Concat(
                [new Message(ChatRole.System, prompt)],
                    (await db.ChatbotContexts.Where(c => c.GroupId == groupId)
                    .OrderByDescending(c => c.Id)
                    .Take(5)
                    .ToListAsync(cancellationToken))
                    .Select(c => JsonSerializer.Deserialize<Message>(c.Context)!))
                .Concat([userMessage]);
            var res = await chatbotSwitchService.Chatbot.ChatAsync(new Chat(chatbotContexts.ToList()), cancellationToken);
            
            
            var interestChat = res
                .GroupBy(c => c.score)
                .Where(c => isBotHasBeenMentioned ? c.Key > 60 : c.Key >= 85)
                .MaxBy(c => c.Key)
                ?.MaxBy(c => c.reply.Length);
            if (interestChat is null) return;

            var messageExists = await db.ChatbotGroupChatHistories
                .Where(c => c.MessageId == interestChat.messageId)
                .AnyAsync(cancellationToken);

            List<MessageBase> pendingSendMessages = [];
            if (messageExists || isBotHasBeenMentioned)
            {
                pendingSendMessages.Add(new QuoteMessage() { MessageId = isBotHasBeenMentioned ? lastMessage.id : interestChat.messageId });
            }

            bool archiveAsForawrdMsg = false;
            if (interestChat.imagePrompt is { Length: > 0 })
            {
                var fileName = $"{Guid.NewGuid().ToString()}.jpg";
                pendingSendMessages.Add(new ImageMessage()
                {
                    Url = $"https://image.pollinations.ai/prompt/{interestChat.imagePrompt}/{fileName}?width=1024&height=1024&seed=100&model=flux-pro&nologo=true&enhance=true"
                });
            }
            
            pendingSendMessages.Add(new PlainMessage($"({interestChat.topic}) {interestChat.reply}"));

            // if (archiveAsForawrdMsg)
            // {
            //     pendingSendMessages = [new ForwardMessage()
            //     {
            //         NodeList = pendingSendMessages.Select(i => new ForwardMessage.ForwardNode()
            //         {
            //             MessageChain = i,
            //             SenderName = "Zerobot",
            //             SenderId = "123456789",
            //             Time = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(8)).ToString("yyyy-MM-dd HH:mm:ss")
            //         }),
            //     }];
            // }
            //
            var sendResults = await MiraiService.SendMessageToSomeGroup([groupId], cancellationToken, pendingSendMessages.ToArray());
            
            await db.ChatbotContexts.AddRangeAsync([
                new ChatbotContext()
                {
                    GroupId = groupId,
                    Context = JsonSerializer.Serialize(userMessage)
                },
                new ChatbotContext()
                {
                    GroupId = groupId,
                    Context = JsonSerializer.Serialize(new Message(ChatRole.Assistant, interestChat.reply))
                },
            ], cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            
            if (sendResults.TryGetValue(groupId, out var result))
            {
                await db.ChatbotGroupChatHistories.AddAsync(new ChatbotGroupChatHistory()
                {
                    GroupId = groupId,
                    UserId = MiraiService.UserId,
                    MessageId = result,
                    Message = interestChat.reply,
                }, cancellationToken);
                await db.SaveChangesAsync(cancellationToken);
            }

        }, cancellationToken);

    }
}