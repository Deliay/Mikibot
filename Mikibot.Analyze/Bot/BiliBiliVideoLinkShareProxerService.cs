using Microsoft.Extensions.Logging;
using Mikibot.Analyze.MiraiHttp;
using Mikibot.Crawler.Http.Bilibili;
using Mirai.Net.Data.Messages;
using Mirai.Net.Data.Messages.Concretes;
using Mirai.Net.Data.Messages.Receivers;
using Mirai.Net.Data.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Mikibot.Analyze.Bot
{
    public class BiliBiliVideoLinkShareProxerService
    {
        public BiliBiliVideoLinkShareProxerService(
            IMiraiService miraiService,
            ILogger<BiliBiliVideoLinkShareProxerService> logger,
            BiliVideoCrawler crawler)
        {
            MiraiService = miraiService;
            Logger = logger;
            Crawler = crawler;
        }

        private IMiraiService MiraiService { get; }
        private ILogger<BiliBiliVideoLinkShareProxerService> Logger { get; }
        private BiliVideoCrawler Crawler { get; }

        private SemaphoreSlim semaphoreSlim = new(1);


        private readonly Channel<GroupMessageReceiver> messageQueue = Channel
        .CreateUnbounded<GroupMessageReceiver>(new UnboundedChannelOptions()
        {
            SingleWriter = true,
            AllowSynchronousContinuations = false,
        });

        public async ValueTask TrySend(Group group, string? bv, string? av, CancellationToken token)
        {
            try
            {
                var result = await Crawler.GetVideoInfo(bv, av == null ? null : int.Parse(av!), token);

                await MiraiService.SendMessageToGroup(group, token, new MessageBase[]
                {
                    new ImageMessage()
                    {
                        Url = result.CoverUrl,
                    },
                    new PlainMessage($"{result.Title} (作者: {result.Owner.Name}) \n https://bilibili.com/{result.BvId}"),
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error while send message");
            }
        }

        private static readonly HashSet<char> ValidBv = new() {
            'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M',
            'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z',
            'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm',
            'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z',
            '1', '2', '3', '4', '5', '6', '7', '8', '9', '0'
        };
        private static readonly HashSet<char> ValidAv = new() {
            '1', '2', '3', '4', '5', '6', '7', '8', '9', '0'
        };
        private static string Fetch(string raw, int startIndex, HashSet<char> allow)
        {
            for (int i = startIndex + 1; i < raw.Length; i++)
            {
                if (!allow.Contains(raw[i]))
                    return raw[startIndex..i];
            }
            return raw[startIndex..];
        }
        private async ValueTask Dequeue(CancellationToken token)
        {
            await foreach (var msg in this.messageQueue.Reader.ReadAllAsync(token))
            {
                var group = msg.Sender.Group;

                foreach (var rawMsg in msg.MessageChain)
                {
                    if (rawMsg is PlainMessage plain)
                    {
                        var text = plain.Text;
                        var bvStart = text.IndexOf("/BV");
                        if (bvStart > -1)
                        {
                            var bv = Fetch(text, bvStart + 1, ValidBv);
                            Logger.LogInformation("准备发送bv {}", bv);
                            await TrySend(group, bv, null, token);
                            break;
                        }

                        var avStart = text.IndexOf("/av");
                        if (avStart > -1)
                        {
                            var av = Fetch(text, avStart + 3, ValidAv);
                            Logger.LogInformation("准备发送av {}", av);
                            await TrySend(group, null, av, token);
                            break;
                        }
                    }
                }

            }
        }

        private void FilterMessage(GroupMessageReceiver message)
        {
            _ = messageQueue.Writer.WriteAsync(message);
        }

        public async Task Run(CancellationToken token)
        {
            Logger.LogInformation("B站连接parser");
            MiraiService.SubscribeMessage(FilterMessage, token);
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await messageQueue.Reader.WaitToReadAsync(token);
                    Logger.LogInformation("started B站连接parser...");
                    await Dequeue(token);
                }
                catch (Exception ex)
                {
                    Logger.LogError("B站连接parserr！", ex);
                }
            }
        }
    }
}
