using Microsoft.Extensions.Logging;
using Mikibot.Crawler.WebsocketCrawler.Packet;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Mikibot.Crawler.WebsocketCrawler.Client
{
    public class RawWebSocketWorker : IDisposable
    {
        private static byte[] ProcessEndian(ref byte[] src)
        {
            if (BitConverter.IsLittleEndian) Array.Reverse(src);

            return src;
        }

        private readonly ClientWebSocket ws = new();
        private CancellationTokenSource csc;
        private SemaphoreSlim _semaphore;

        public RawWebSocketWorker(ILogger<RawWebSocketWorker> logger)
        {
            Logger = logger;
            _semaphore = new(1);
            _semaphore.Wait();
        }

        public ILogger<RawWebSocketWorker> Logger { get; }

        public async ValueTask ConnectAsync(string host, int port, int roomId, string auth, CancellationToken token)
        {
            csc = CancellationTokenSource.CreateLinkedTokenSource(token);
            var safeToken = csc.Token;

            await ws.ConnectAsync(new UriBuilder(Uri.UriSchemeWs, host, port).Uri, safeToken);

            await SendAsync(BasePacket.Auth(roomId, auth), safeToken);

            Keeplive(safeToken).Start();

            _semaphore.Release();
        }

        private async ValueTask SendAsync(BasePacket packet, CancellationToken token)
        {
            await ws.SendAsync(packet, WebSocketMessageType.Binary, true, token);
        }

        private async ValueTask SendAsync(ArraySegment<byte> packet, CancellationToken token)
        {
            await ws.SendAsync(packet, WebSocketMessageType.Binary, true, token);
        }

        private async Task Keeplive(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(30), token);
                await SendAsync(BasePacket.Keeplive(), token);
            }
        }

        public async IAsyncEnumerable<BasePacket> ReadPacket([EnumeratorCancellation]CancellationToken token)
        {
            await _semaphore.WaitAsync(token);
            while (ws.State == WebSocketState.Open && token.IsCancellationRequested == false)
            {
                using MemoryStream ms = new(4096);
                var buffer = new byte[4096];
                WebSocketReceiveResult result;
                do
                {
                    result = await ws.ReceiveAsync(buffer, token);
                    ms.Write(buffer, 0, result.Count);
                } while (!result.EndOfMessage);

                yield return ms.GetBuffer();
            }
        }

        public void Dispose()
        {
            using var socket = this.ws;
            using var csc = this.csc;
        }
    }
}
