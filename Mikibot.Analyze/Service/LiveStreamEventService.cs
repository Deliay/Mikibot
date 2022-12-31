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
            Logger.LogInformation("准备连接到房间: {}....", mxmk);
            var realRoomId = await Crawler.GetRealRoomId(mxmk, token);
            var spectatorEndpoint = await Crawler.GetLiveToken(realRoomId, token);
            var rand = new Random();

            foreach (var spectatorHost in spectatorEndpoint.Hosts)
            {
                var next = rand.Next(2, 10);

                Logger.LogInformation("{} 秒后 准备连接到弹幕服务器: wss://{}:{}....", next, spectatorHost.Host, spectatorHost.Port);
                try
                {
                    using var wsClient = new WebsocketClient();

                    await wsClient.ConnectAsync(spectatorHost.Host, spectatorHost.WssPort, realRoomId, spectatorEndpoint.Token, "wss", cancellationToken: token);
                    Logger.LogInformation("弹幕连接到房间: wss://{}:{}....连接成功", spectatorHost.Host, spectatorHost.Port);

                    await foreach (var @event in wsClient.Events(token))
                    {
                        await CmdHandler.Handle(@event);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogInformation(ex, "弹幕服务器: wss://{}:{}....处理时发生错误！将尝试下一个房间", spectatorHost.Host, spectatorHost.Port);
                }
            }
        }

        public async Task Run(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await ConnectAsync(token);
                }
                catch (Exception ex)
                {
                    // 每次间隔 失败次数 * 3 秒再尝试下一次连接弹幕
                    await Task.Delay(++failedRetry * TimeSpan.FromSeconds(3), token);
                    // 这里失败25次会清空重试次数，最长等待时间是 75秒
                    if (failedRetry >= 25) failedRetry = 0;

                    Logger.LogError(ex, "在抓弹幕的时候发生异常");
                }
            }
        }
    }
}
