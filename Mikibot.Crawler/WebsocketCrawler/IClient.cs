using Mikibot.Crawler.WebsocketCrawler.Data;
using Mikibot.Crawler.WebsocketCrawler.Packet;

namespace Mikibot.Crawler.WebsocketCrawler
{
    public interface IClient
    {
        public int RoomId { get; }

        public ValueTask<bool> ConnectAsync(int roomId, CancellationToken token);

        public IAsyncEnumerable<IData> Events(CancellationToken token);
    }
}