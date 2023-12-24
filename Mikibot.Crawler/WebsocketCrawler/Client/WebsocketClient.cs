using Microsoft.Extensions.Logging;
using Mikibot.Crawler.Http.Bilibili;
using Mikibot.Crawler.WebsocketCrawler.Client;
using Mikibot.Crawler.WebsocketCrawler.Data;
using Mikibot.Crawler.WebsocketCrawler.Packet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Mikibot.Crawler.WebsocketCrawler.Client
{
    public class WebsocketClient : IClient, IDisposable
    {
        private readonly RawWebSocketWorker _worker;
        private readonly CancellationTokenSource _csc;
        public WebsocketClient()
        {
            _worker = new();
            _csc = new();
        }

        public long RoomId { get; private set; }

        public ValueTask<bool> ConnectAsync(string host, int port, long roomId, long uid, string liveToken, string protocol = "ws", CancellationToken cancellationToken = default)
        {
            return ConnectAsync(null!, host, port, roomId, uid, liveToken, protocol, cancellationToken);
        }
        public async ValueTask<bool> ConnectAsync(HttpMessageInvoker invoker, string host, int port, long roomId, long uid, string liveToken, string protocol = "ws", CancellationToken cancellationToken = default)
        {
            var connectCsc = CancellationTokenSource.CreateLinkedTokenSource(_csc.Token, cancellationToken);
            await _worker.ConnectAsync(invoker, host, port, roomId, uid, liveToken, protocol, connectCsc.Token);
            return true;
        }

        private static byte[] Unpack(byte[] data, Func<Stream, Stream> uncompresser)
        {
            using var ms = new MemoryStream(data);
            using var zlib = uncompresser(ms);
            using var unpack = new MemoryStream(data.Length);

            zlib.CopyTo(unpack);
            var buffer = unpack.GetBuffer();
            return buffer;
        }

        private static Stream Zlib(Stream data) => new ZLibStream(data, CompressionMode.Decompress);
        private static Stream Brotli(Stream data) => new BrotliStream(data, CompressionMode.Decompress);

        public static IEnumerable<IData> ProcessPacket(ReadOnlyMemory<byte> raw)
        {
            var packet = BasePacket.ToPacket(raw);
            if (packet.Size == 0) yield break;

            var extractedRaw = packet.Version switch
            {
                ProtocolVersion.BrotliCompressed => Unpack(packet.Data, Brotli),
                ProtocolVersion.ZlibCompressed => Unpack(packet.Data, Zlib),
                _ => raw,
            };
            var extractedPacket = BasePacket.ToPacket(extractedRaw);

            if (extractedRaw.Length <= extractedPacket.Size)
            {
#if DEBUG
                Debug.WriteLineIf(extractedRaw.Length < extractedPacket.Size,
                    () => $"[Packet] Invalid packet length, except={extractedPacket.Size}, actual={extractedRaw.Length}");
#endif
                yield return DataTypeMapping.Parse(extractedPacket, extractedPacket.Data);
                yield break;
            }

            BasePacket headPacket = BasePacket.ToPacket(extractedRaw[..(int)extractedPacket.Size]);

            yield return DataTypeMapping.Parse(headPacket, headPacket.Data);

            if (extractedRaw.Length > headPacket.Size)
            {
                var restRaw = extractedRaw[(int)headPacket.Size..];
                if (restRaw.Length > 17)
                {
                    foreach (var data in ProcessPacket(restRaw))
                    {
                        yield return data;
                    }
                }
            }
        }

        public async IAsyncEnumerable<IData> Events([EnumeratorCancellation]CancellationToken token)
        {
            var connectCsc = CancellationTokenSource.CreateLinkedTokenSource(_csc.Token, token);
            await foreach (var raw in _worker.ReadPacket(connectCsc.Token))
            {
                foreach (var data in ProcessPacket(raw))
                {
                    if (token.IsCancellationRequested) yield break;

                    yield return data;
                }
            }
        }

        public void Dispose()
        {
            using var worker = this._worker;
            using var csc = this._csc;
            GC.SuppressFinalize(this);

        }
    }
}
