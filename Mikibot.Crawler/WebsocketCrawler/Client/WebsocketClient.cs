using Microsoft.Extensions.Logging;
using Mikibot.Crawler.Http.Bilibili;
using Mikibot.Crawler.WebsocketCrawler.Client;
using Mikibot.Crawler.WebsocketCrawler.Data;
using Mikibot.Crawler.WebsocketCrawler.Package;
using Mikibot.Crawler.WebsocketCrawler.Packet;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Mikibot.Crawler.WebsocketCrawler.Client
{
    public class WebsocketClient : IClient
    {
        private readonly RawWebSocketWorker _worker;
        public WebsocketClient(BiliLiveCrawler crawler, ILogger<WebsocketClient> logger, ILogger<RawWebSocketWorker> workerLogger)
        {
            Crawler = crawler;
            Logger = logger;
            _worker = new RawWebSocketWorker(workerLogger);
        }

        public int RoomId { get; private set; }
        public BiliLiveCrawler Crawler { get; }
        public ILogger<WebsocketClient> Logger { get; }

        public async ValueTask<bool> ConnectAsync(int roomId, CancellationToken token)
        {
            var realRoomId = await Crawler.GetRealRoomId(roomId, token);
            var liveToken = await Crawler.GetLiveToken(realRoomId, token);
            var host = liveToken.Hosts[0];
            await _worker.ConnectAsync(host.Host, host.WsPort, realRoomId, liveToken.Token, token);

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

        private IEnumerable<IData> ProcessPacket(byte[] raw)
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

            if (raw.Length == extractedPacket.GetSize())
            {
                yield return DataTypeMapping.Parse(extractedPacket, extractedPacket.Data);
                yield break;
            }
                

            Logger.LogInformation("packet readed, protocol={}, type={}", extractedPacket.Version, extractedPacket.Type);
            var safeExtractedPacket = BasePacket.ToPacket(extractedRaw[..(int)extractedPacket.Size]);

            yield return DataTypeMapping.Parse(safeExtractedPacket, safeExtractedPacket.Data);

            if (extractedRaw.Length > extractedPacket.GetSize())
            {
                var restRaw = extractedRaw[(int)extractedPacket.Size..];
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
            await foreach (var raw in _worker.ReadPacket(token))
            {
                foreach (var data in ProcessPacket(raw))
                {
                    yield return data;
                }
            }
        }
    }
}
