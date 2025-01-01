using System.IO.Compression;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Mikibot.Analyze.Generic;
using Mikibot.Analyze.MiraiHttp;
using Mikibot.Crawler.Http.Bilibili;
using Mirai.Net.Data.Messages.Concretes;
using Mirai.Net.Data.Messages.Receivers;
using Mirai.Net.Data.Shared;

namespace Mikibot.Analyze.Bot;

public class BiliBiliVideoLinkShareProxyService(
    IMiraiService miraiService,
    ILogger<BiliBiliVideoLinkShareProxyService> logger,
    PermissionService permissions,
    HttpClient client,
    BiliVideoCrawler crawler)
    : MiraiGroupMessageProcessor<BiliBiliVideoLinkShareProxyService>(miraiService, logger)
{
    private async ValueTask TrySend(Group group, string? bv, string? av, CancellationToken token)
    {
        try
        {
            var result = await crawler.GetVideoInfo(bv, av == null ? null : int.Parse(av!), token);

            await MiraiService.SendMessageToGroup(group, token,
            [
                new ImageMessage()
                {
                    Url = result.CoverUrl,
                },
                new PlainMessage($"{result.Title} (作者: {result.Owner.Name}) \n https://bilibili.com/{result.BvId}"),
            ]);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error while send message");
        }
    }

    private static readonly HashSet<char> ValidBv = [
        'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M',
        'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z',
        'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm',
        'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z',
        '1', '2', '3', '4', '5', '6', '7', '8', '9', '0'
    ];
    private static readonly HashSet<char> ValidAv = [
        '1', '2', '3', '4', '5', '6', '7', '8', '9', '0'
    ];
    private static string Fetch(string raw, int startIndex, HashSet<char> allow)
    {
        for (int i = startIndex + 1; i < raw.Length; i++)
        {
            if (!allow.Contains(raw[i]))
                return raw[startIndex..i];
        }
        return raw[startIndex..];
    }

    private static readonly IEnumerable<string> Selectors =
    [
        "//meta[@property='og:image']",
        "//meta[@property='og:url']",
        "//meta[@name='title']",
        "//meta[@name='author']"
    ];
    
    private async ValueTask TrySend(Group group, string url, CancellationToken token = default)
    {
        var doc = new HtmlDocument();
        await using var contentStream = await client.GetStreamAsync(url, token);
        await using var uncompressedStream = new GZipStream(contentStream, CompressionMode.Decompress);
        doc.Load(uncompressedStream);

        var infoList = Selectors
            .Select(s => doc.DocumentNode.SelectSingleNode(s))
            .Select(s => s.Attributes["content"].Value)
            .ToList();

        var image = infoList[0];
        var bvUrl = infoList[1];
        var title = infoList[2];
        var author = infoList[3];
        
        await MiraiService.SendMessageToGroup(group, token,
        [
            new ImageMessage()
            {
                Url = bvUrl,
            },
            new PlainMessage($"{title} (作者: {author}) \n {bvUrl}"),
        ]);
    }

    private const string BiliVideoParser = "BiliVideoParser";
    protected override async ValueTask Process(GroupMessageReceiver message, CancellationToken token = default)
    {
        var group = message.Sender.Group;
        
        foreach (var rawMsg in message.MessageChain)
        {
            if (rawMsg is PlainMessage plain)
            {
                var text = plain.Text;
                var bvStart = text.IndexOf("/BV", StringComparison.InvariantCulture);
                if (bvStart > -1)
                {
                    if (!await permissions.IsGroupEnabled(BiliVideoParser, group.Id, token)) return;
                    var bv = Fetch(text, bvStart + 1, ValidBv);
                    Logger.LogInformation("准备发送bv {}", bv);
                    await TrySend(group, bv, null, token);
                    return;
                }

                var avStart = text.IndexOf("/av", StringComparison.InvariantCulture);
                if (avStart > -1)
                {
                    if (!await permissions.IsGroupEnabled(BiliVideoParser, group.Id, token)) return;
                    var av = Fetch(text, avStart + 3, ValidAv);
                    Logger.LogInformation("准备发送av {}", av);
                    await TrySend(group, null, av, token);
                    return;
                }

                const string b23raw = "https://b23.tv/";
                const string b2233raw = "https://bili2233.cn/";
                var b23 = text.IndexOf(b23raw, StringComparison.InvariantCulture);
                var b2233 = text.IndexOf(b2233raw, StringComparison.InvariantCulture);
                if (b23 > -1 || b2233 > -1)
                {
                    if (!await permissions.IsGroupEnabled(BiliVideoParser, group.Id, token)) return;
                    var pos = b23 > -1 ? b23 : b2233;
                    var prefix = b23 > -1 ? b23raw : b2233raw;
                    var suffix = Fetch(text, pos + prefix.Length, ValidBv);
                    var url = prefix + suffix;
                    Logger.LogInformation("准备发送url {}", url);
                    await TrySend(group, url, token);
                    return;
                }
            }
        }
    }
}