using Mikibot.Crawler.WebsocketCrawler.Data;

namespace Mikibot.Crawler.WebsocketCrawler;

public interface IClient
{
    public long RoomId { get; }

    public ValueTask<bool> ConnectAsync(string host, int port, long roomId, long uid, string liveToken, string protocol, CancellationToken cancellationToken = default);

    public IAsyncEnumerable<IData> Events(CancellationToken token);
}