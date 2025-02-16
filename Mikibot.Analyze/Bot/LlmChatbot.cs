﻿using Microsoft.EntityFrameworkCore;
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

namespace Mikibot.Analyze.Bot;

public class LlmChatbot(
    IMiraiService miraiService,
    ILogger<LlmChatbot> logger,
    PermissionService permissions,
    MikibotDatabaseContext db,
    ChatbotSwitchService chatbotSwitchService)
    : MiraiGroupMessageProcessor<LlmChatbot>(miraiService, logger)
{
    private readonly Dictionary<string, Queue<string>> _recentMessages = [];
    private readonly Dictionary<string, SemaphoreSlim> _locks = [];
    private readonly Dictionary<string, string> lastSubmitMessage = [];
    private readonly Dictionary<string, DateTimeOffset> lastAtAt = [];

    private const string BasicPrompt =
        "以上面的人设作为角色设定，分析user提供的聊天记录，一行是一句发言。" +
        "请在上下文中精炼出最多3个话题，给出回复，字数可以1-25字不等，灵活安排回复内容，要长时长，需短时短。" +
        "回复措辞和内容口语化表达。以JSON数组的形式输出，以JSON数组的形式输出。" +
        "JSON格式为：[{ \"topic\": \"推测的话题\", \"reply\": \"回复的消息\", \"score\": 角色设定对话题的兴趣分数(0-100)  },...]";

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
                    messages.Enqueue($"{message.Sender.Name}: {plain.Text}\n");
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
        if (!_locks.TryGetValue(groupId, out var _lock))
        {
            _locks.Add(groupId, _lock = new SemaphoreSlim(1));
        }
        return _lock;
    }

    private async ValueTask BeginLock(string groupId, Func<CancellationToken, ValueTask> func, CancellationToken cancellationToken)
    {
        var _lock = GetLock(groupId);

        await _lock.WaitAsync(cancellationToken);
        try
        {
            await func(cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "A error thrown while lock scope executing...");
        }
        finally
        {
            _lock.Release();
        }
    }

    private async ValueTask TryResponse(string groupId, string userId, bool ignoreMessageCount = false, CancellationToken cancellationToken = default)
    {

        if (!_recentMessages.TryGetValue(groupId, out var messages)) return;

        await BeginLock(groupId, async (cancellationToken) =>
        {
            if (!ignoreMessageCount && messages.Count < 15) return;
            // 最低也要2条
            if (ignoreMessageCount && messages.Count < 2) return;

            // add 15 seconds colddown to prevent spam
            if (ignoreMessageCount)
            {
                if (lastAtAt.TryGetValue(groupId, out var at))
                {
                    if (DateTimeOffset.Now -  at < TimeSpan.FromSeconds(5))
                    {
                        return;
                    }
                }
            }

            string messageList = "";
            string lastMessage = "";

            while (messages.TryDequeue(out var message))
            {
                messageList += message;
                lastMessage = message;
            }

            lastSubmitMessage.Remove(groupId);
            lastSubmitMessage.Add(groupId, messageList);

            if (ignoreMessageCount)
            {
                lastAtAt.Remove(groupId);
                lastAtAt.Add(groupId, DateTimeOffset.Now);
            }

            if (ignoreMessageCount && lastSubmitMessage.TryGetValue(groupId, out var msg))
            {

                var recentMessage = string.Join('\n', await db.ChatbotGroupChatHistories
                    .Where(c => c.GroupId == groupId && c.UserId == userId)
                    .OrderByDescending(c => c.Id)
                    .Take(5)
                    .Select(c => $"- {c.Message}")
                    .Reverse()
                    .ToListAsync(cancellationToken));
                
                messageList = msg + "\n" + messageList
                + "你之前的发言被下面这个人回复了，他这段时间的发言如下，这里发言仅供参考：" + recentMessage +
                "\n\n特别针对下面这条消息给出回应：\n"
                + lastMessage;
            }

            var prompt = await GetPrompt(groupId, cancellationToken);
            var userPrompot = messageList;

            Logger.LogInformation("prompt: {}", prompt);
            Logger.LogInformation("user prompt: {}", userPrompot);

            var res = await chatbotSwitchService.Chatbot.ChatAsync(new Chat(
            [
                new Message(ChatRole.System, prompt),
                new Message(ChatRole.User, userPrompot)
            ]), cancellationToken);
            
            
            var interestChat = res.MaxBy(c => c.score);
            if (interestChat is null) return;
            
            if (ignoreMessageCount)
                messages.Enqueue(interestChat.reply + "\n");

            await MiraiService.SendMessageToSomeGroup([groupId], cancellationToken,
                new PlainMessage(interestChat.reply));

        }, cancellationToken);

    }
}