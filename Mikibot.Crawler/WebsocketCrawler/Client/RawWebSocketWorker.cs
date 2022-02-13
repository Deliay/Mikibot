using Microsoft.Extensions.Logging;
using Mikibot.Crawler.WebsocketCrawler.Packet;
using Mikibot.Crawler.WebsocketCrawler.Packet;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
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

        public RawWebSocketWorker()
        {
            _semaphore = new(1);
            _semaphore.Wait();
        }

        public async ValueTask ConnectAsync(string host, int port, int roomId, string auth, string protocol, CancellationToken token)
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
            await SendAsync(packet.ToByte(), token);
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

        public async IAsyncEnumerable<byte[]> ReadPacket([EnumeratorCancellation] CancellationToken token)
        {
            await _semaphore.WaitAsync(token);
            while (ws.State == WebSocketState.Open && !token.IsCancellationRequested && !csc.IsCancellationRequested)
            {
                ValueWebSocketReceiveResult result;
                do
                {
                    var buffer = new Memory<byte>(new byte[4096]);
                    result = await ws.ReceiveAsync(buffer, token);
#if DEBUG
                    var lengthBE = BitConverter.ToUInt32(buffer[..4].ToArray());
                    var length = ((lengthBE & 0x000000FF) << 24)
                        | ((lengthBE & 0x0000FF00) << 8)
                        | ((lengthBE & 0x00FF0000) >> 8)
                        | ((lengthBE & 0xFF000000) >> 24);
                    if (length != result.Count)
                        Debug.WriteLine("[Socket] packet required={0} socket receive={1}", length, result.Count);
#endif
                    yield return buffer[..result.Count].ToArray();
                } while (!result.EndOfMessage);

                //var data = ms.ToArray();
                //while (data.Length > 0)
                //{
                //    var lengthRaw = data[..4]; Array.Reverse(lengthRaw);
                //    var length = (int)BitConverter.ToUInt32(lengthRaw);
                //    Debug.WriteLine("[Packet] required length {0}, current size = {1}", length, data.Length);
                //    if (data.Length >= length)
                //    {
                //        yield return data[..length];
                //        //Debug.WriteLine("Packet payload loaded, dispatched data length={0}, remain=", length, data.Length);
                //        data = data[length..];
                //    }
                //    else
                //    {
                //        using var _ms = ms;
                //        ms = new();
                //        ms.Write(data);
                //        Debug.WriteLine("[Packet] payload still loading...require={0}, actual={1}", length, ms.Length);
                //        break;
                //    }
                //    if (data.Length == 0)
                //    {
                //        using var _ms = ms;
                //        ms = new();
                //    }
                //}
            }
        }

        public void Dispose()
        {
            using var socket = this.ws;
            using var csc = this.csc;
            GC.SuppressFinalize(this);
        }
    }
}
