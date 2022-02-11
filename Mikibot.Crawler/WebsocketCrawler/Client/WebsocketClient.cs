﻿using Microsoft.Extensions.Logging;
using Mikibot.Crawler.Http.Bilibili;
using Mikibot.Crawler.WebsocketCrawler.Client;
using Mikibot.Crawler.WebsocketCrawler.Packet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mikibot.Crawler.WebsocketCrawler.Client
{
    public class WebsocketClient : IClient
    {
        private RawWebSocketWorker _worker;
        private SemaphoreSlim _semaphore;
        public WebsocketClient(BiliLiveCrawler crawler, ILogger<WebsocketClient> logger, ILogger<RawWebSocketWorker> workerLogger)
        {
            Crawler = crawler;
            Logger = logger;
            _worker = new RawWebSocketWorker(workerLogger);
            _semaphore = new SemaphoreSlim(1);
            _semaphore.Wait();
        }

        public int RoomId { get; private set; }
        public BiliLiveCrawler Crawler { get; }
        public ILogger<WebsocketClient> Logger { get; }

        public async ValueTask<bool> ConnectAsync(int roomId, CancellationToken token)
        {
            var liveToken = await Crawler.GetLiveToken(roomId, token);
            var host = liveToken.Hosts[0];
            await _worker.ConnectAsync(host.Host, host.WsPort, roomId, liveToken.Token, token);

            return true;
        }

        public async IAsyncEnumerable<BasePacket> Events(CancellationToken token)
        {
            await _semaphore.WaitAsync(token);
            

        }
    }
}
