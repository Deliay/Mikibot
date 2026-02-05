using System.Runtime.CompilerServices;
using Mikibot.Crawler.Http.Bilibili;
using Mikibot.Crawler.Http.Bilibili.Model.LiveServer;
using Mikibot.Crawler.WebsocketCrawler.Data;

namespace Mikibot.Crawler.WebsocketCrawler.Client;

public class DanmakuClient(WebsocketClient client, HttpClient httpClient, BilibiliAccount account, BiliLiveCrawler liveCrawler) : IDisposable
{
    public async ValueTask<bool> ConnectAsync(long roomId, Func<List<LiveServerInfo>, LiveServerInfo> hostSelector, CancellationToken cancellationToken = default)
    {
        var playAddr = await liveCrawler.GetLiveStreamAddressV2(roomId, cancellationToken);
        var realRoomId = playAddr.RoomId;
        var danmakuInfo = await liveCrawler.GetLiveToken(realRoomId, cancellationToken);
        var host = hostSelector(danmakuInfo.Hosts);

        return await client.ConnectAsync(httpClient,
            host: host.Host,
            port: host.WssPort > 0 ? host.WssPort : host.Port,
            roomId: realRoomId,
            uid: account.Mid,
            liveToken: danmakuInfo.Token,
            protocol: host.WssPort > 0 ? "wss" : "ws",
            cancellationToken: cancellationToken);
    }

    public ValueTask<bool> ConnectAsync(long roomId, CancellationToken cancellationToken = default)
    {
        return ConnectAsync(roomId, (hosts) => hosts.Count == 0
            ? throw new InvalidOperationException("No danmaku server returned from bilibili")
            : hosts[0], cancellationToken);
    }

    public IAsyncEnumerable<IData> Events(CancellationToken cancellationToken = default)
    {
        return client.Events(cancellationToken);
    }

    public void Dispose()
    {
        client.Dispose();
    }
}
