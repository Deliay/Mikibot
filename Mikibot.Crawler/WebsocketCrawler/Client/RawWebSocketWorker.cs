using Microsoft.Extensions.Logging;
using Mikibot.Crawler.WebsocketCrawler.Package;
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
        private readonly ClientWebSocket ws = new();
        private CancellationTokenSource csc;
        private readonly SemaphoreSlim _semaphore;

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

            var uri = new UriBuilder(Uri.UriSchemeWs, host, port, "/sub").Uri;

            await ws.ConnectAsync(uri, safeToken);

            await SendAsync(BasePacket.Auth(roomId, auth), safeToken);

            _ = Keeplive(safeToken);

            _semaphore.Release();
        }

        private async ValueTask SendAsync(BasePacket packet, CancellationToken token)
        {
            Logger.LogInformation("packet sent: type={}, datastr={}", packet.Type, Encoding.UTF8.GetString(packet.Data));
            await SendAsync(packet.ToByte(), token);
        }

        private async ValueTask SendAsync(ArraySegment<byte> packet, CancellationToken token)
        {
            Logger.LogInformation("packet sent: size={}", packet.Count);
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

        public async IAsyncEnumerable<byte[]> ReadPacket([EnumeratorCancellation] CancellationToken token)
        {
            await _semaphore.WaitAsync(token);
            while (ws.State == WebSocketState.Open && !token.IsCancellationRequested && !csc.IsCancellationRequested)
            {
                ValueWebSocketReceiveResult result;
                MemoryStream ms = new();
                var buffer = new Memory<byte>(new byte[4096]);
                do
                {
                    result = await ws.ReceiveAsync(buffer, token);
                    ms.Write(buffer[..result.Count].Span);

                } while (!result.EndOfMessage);

                var data = ms.ToArray();
                while (data.Length > 0)
                {
                    var lengthRaw = data[..4]; Array.Reverse(lengthRaw);
                    var length = (int)BitConverter.ToUInt32(lengthRaw);
                    if (data.Length >= length)
                    {
                        yield return data[..length];
                        data = data[length..];
                    }
                    else
                    {
                        using var _ms = ms;
                        ms = new MemoryStream(data);
                        break;
                    }
                }
            }
        }

        public void Dispose()
        {
            using var socket = this.ws;
            using var csc = this.csc;
        }
    }
}
