using Microsoft.Extensions.Logging;
using Mikibot.Analyze.Notification;
using Mikibot.Crawler.Http.Bilibili;
using Mikibot.Crawler.WebsocketCrawler.Client;
using Mikibot.Crawler.WebsocketCrawler.Data.Commands.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mikibot.Analyze.Service
{
    public class LiveStreamEventService
    {
        public const int mxmk = BiliLiveCrawler.mxmkr;
        public CommandSubscriber CmdHandler { get; }
        public ILogger<DanmakuCollectorService> Logger { get; }
        public BiliLiveCrawler Crawler { get; }

        public LiveStreamEventService(ILogger<DanmakuCollectorService> logger, BiliLiveCrawler crawler)
        {
            CmdHandler = new CommandSubscriber();
            Logger = logger;
            Crawler = crawler;
        }

        private int failedRetry = 0;

        private async Task ConnectAsync(CancellationToken token)
        {
            using var wsClient = new WebsocketClient();

            Logger.LogInformation("准备连接到房间: {}....", mxmk);
            var realRoomId = await Crawler.GetRealRoomId(mxmk, token);
            var spectatorEndpoint = await Crawler.GetLiveToken(realRoomId, token);
            var spectatorHost = spectatorEndpoint.Hosts[0];

            Logger.LogInformation("准备连接到服务器: ws://{}:{}....", spectatorHost.Host, spectatorHost.Port);
            await wsClient.ConnectAsync(spectatorHost.Host, spectatorHost.WsPort, realRoomId, spectatorEndpoint.Token, cancellationToken: token);

            Logger.LogInformation("准备连接到房间: ws://{}:{}....连接成功", spectatorHost.Host, spectatorHost.Port);
            await foreach (var @event in wsClient.Events(token))
            {
                await CmdHandler.Handle(@event);
            }
        }

        public async Task Run(CancellationToken token)
        {
            while (!token.IsCancellationRequested && failedRetry++ < 5)
            {
                try
                {
                    await ConnectAsync(token);
                    failedRetry -= 1;
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "在抓弹幕的时候发生异常");
                }
            }
        }
    }
}
