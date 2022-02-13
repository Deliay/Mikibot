using Mikibot.Crawler.WebsocketCrawler.Data;
using Mikibot.Crawler.WebsocketCrawler.Packet;

namespace Mikibot.Crawler.WebsocketCrawler
{
    public interface IClient
    {
        public int RoomId { get; }

        public ValueTask<bool> ConnectAsync(string host, int port, int roomId, string liveToken, string protocol, CancellationToken cancellationToken = default);

        public IAsyncEnumerable<IData> Events(CancellationToken token);
    }
}