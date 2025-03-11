using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Mikibot.Analyze.Generic;
using Mikibot.Analyze.MiraiHttp;
using Mirai.Net.Data.Messages.Concretes;
using Mirai.Net.Data.Messages.Receivers;

namespace Mikibot.Analyze.Bot;

public class PingtiItemReplaceService(IQqService qqService, ILogger<PingtiItemReplaceService> logger)
    : MiraiGroupMessageProcessor<PingtiItemReplaceService>(qqService, logger)
{
    private readonly HttpClient client = new()
    {
        DefaultRequestHeaders =
        {
            {"origin", "https://www.pingti.xyz"},
            {"pragma", "no-cache"},
            {"referer", "https://www.pingti.xyz/"},
            {"sec-ch-ua", "\"Not_A Brand\";v=\"8\", \"Chromium\";v=\"120\", \"Microsoft Edge\";v=\"120\""},
            {"sec-ch-ua-mobile", "?0"},
            {"sec-ch-ua-platform", "\"Linux\""},
            {"accept", "*/*"},
            {"accept-language", "en,zh-CN;q=0.9,zh;q=0.8,en-GB;q=0.7,en-US;q=0.6"},
            {"cache-control", "no-cache"},
            {"user-agent", "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36 Edg/120.0.0.0"},
            {"cookie", "cf_clearance=Sinygclcc1o7JXKQUGMwupF2cKeaPvhnD0Hl7mUy5lw-1705043751-0-2-84a182b5.a081298a.d9b532ed-0.2.1705043751; _ga=GA1.1.1591650678.1705043753; _ga_3E02DMZGKH=GS1.1.1705050411.2.1.1705050843.0.0.0"}
        },
        Timeout = TimeSpan.FromSeconds(10),
    };
    private readonly SemaphoreSlim _lock = new(1);
    private static readonly string CacheFile = Path.Combine(Environment.GetEnvironmentVariable("MIKI_VOICE_DIR") ?? "", "pingti.json");
    private readonly Dictionary<string, string> Cache = File.Exists(CacheFile)
        ? JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(CacheFile)) ?? []
        : [];

    private readonly record struct PingtiResult(string response, string reason);
    private async ValueTask<string> GetReplaceItemRequest(string raw, CancellationToken token = default)
    {
        var msgStr = "{\"messages\":[{\"role\":\"user\",\"content\":" + JsonSerializer.Serialize(raw) + "}]}";
        var res = await client.PostAsync("https://www.pingti.xyz/api/chat", new StringContent(msgStr, null, "text/plain"), token);
        
        res.EnsureSuccessStatusCode();

        var result = await res.Content.ReadFromJsonAsync<PingtiResult>(token);

        return $"{result.response}！\n因为{result.reason}！";
    }
    private async ValueTask<string> GetReplaceItem(string raw, CancellationToken token = default)
    {
        await _lock.WaitAsync(token);
        try
        {
            if (!Cache.TryGetValue(raw, out string? value))
            {
                Cache.Add(raw, value = await GetReplaceItemRequest(raw, token));
                await File.WriteAllTextAsync(CacheFile, JsonSerializer.Serialize(Cache), token);
            }

            return value;
        }
        catch (Exception e)
        {
            return $"获取失败 {e.Message}";
        }
        finally
        {
            _lock.Release();
        }
        
    }

    private readonly Dictionary<string, List<PlainMessage>> messageCache = [];
    private readonly Dictionary<string, DateTime> lastSendTime = [];
    private readonly SemaphoreSlim _lockSend = new(1);

    protected override ValueTask PreRun(CancellationToken token)
    {
        _ = Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(5), token);
                await _lockSend.WaitAsync();
                try
                {
                    foreach (var (groupId, messages) in messageCache)
                    {
                        if (messages.Count > 0)
                        {
                            Logger.LogInformation($"处理聚合消息 {groupId} 共 {messages.Count} 条");
                            await QqService.SendMessageToSomeGroup([groupId], token, messages.ToArray());
                            messages.Clear();
                        }
                    }
                }
                finally
                {
                    _lockSend.Release();
                }
            }
        }, token);

        return ValueTask.CompletedTask;
    }

    protected override async ValueTask Process(GroupMessageReceiver message, CancellationToken token = default)
    {
        foreach (var msg in message.MessageChain)
        {
            if (msg is PlainMessage plain && (plain.Text.StartsWith("!平替") || plain.Text.StartsWith("！平替"))  && plain.Text.Length > 3)
            {
                await _lockSend.WaitAsync(token);
                try
                {
                    var item = plain.Text[3..].Trim();
                    var replace = await GetReplaceItem(item, token);

                    if (!messageCache.TryGetValue(message.GroupId, out var cachedMsgs))
                    {
                        messageCache.Add(message.GroupId, cachedMsgs = []);
                    }

                    cachedMsgs.Add(new PlainMessage($"{item} 的平替是 {replace}"));
                    if (!lastSendTime.TryGetValue(message.GroupId, out var lastSendAt))
                    {
                        lastSendTime.Add(message.GroupId, lastSendAt = DateTime.Now - TimeSpan.FromSeconds(5));
                    }

                    if (DateTime.Now - lastSendAt > TimeSpan.FromSeconds(5))
                    {
                        await QqService.SendMessageToGroup(message.Sender.Group, token, messageCache[message.GroupId].ToArray());
                        messageCache[message.GroupId].Clear();
                        lastSendTime[message.GroupId] = DateTime.Now;
                    }
                }
                finally
                {
                    _lockSend.Release();
                }
            }
        }
    }
}
