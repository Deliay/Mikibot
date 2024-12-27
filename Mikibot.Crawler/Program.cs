using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mikibot.Crawler.Http.Bilibili;
using Mikibot.Crawler.WebsocketCrawler.Client;
using Mikibot.Crawler.WebsocketCrawler.Data.Commands.KnownCommand;
using Mikibot.Crawler.WebsocketCrawler.Data.Commands.Utils;

var serviceBuilder = new ServiceCollection();
serviceBuilder.AddLogging(b => b.AddConsole());
serviceBuilder.AddSingleton<HttpClient>();
serviceBuilder.AddSingleton<BiliLiveCrawler>();
serviceBuilder.AddSingleton<BiliBasicInfoCrawler>();
serviceBuilder.AddTransient<WebsocketClient>();

await using var services = serviceBuilder.BuildServiceProvider();
using var csc = new CancellationTokenSource();

var logger = services.GetRequiredService<ILogger<Program>>();
var crawler = services.GetRequiredService<BiliLiveCrawler>();
var personalCrawler = services.GetRequiredService<BiliBasicInfoCrawler>();
// 必须设置cookie
crawler.SetCookie("....");
var roomId = 1603600;

var self = await personalCrawler.GetNavInfo(csc.Token);
var wsClient = services.GetRequiredService<WebsocketClient>();
var playAddr = await crawler.GetLiveStreamAddressV2(roomId, csc.Token);
var realRoomId = playAddr.RoomId;
var spectatorEndpoint = await crawler.GetLiveToken(realRoomId, csc.Token);

foreach (var spectatorHost in spectatorEndpoint.Hosts)
{
    try
    {
        await wsClient.ConnectAsync(crawler.Client,
            host: spectatorHost.Host,
            port: spectatorHost.WssPort,
            roomId: realRoomId,
            uid: self.Mid,
            liveToken: spectatorEndpoint.Token,
            protocol: "wss",
            cancellationToken: csc.Token);
        break;
    }
    catch (Exception e)
    {
        Console.WriteLine($"连接 {spectatorHost.Host} 失败");
    }
}

Console.WriteLine($"已连接到 {realRoomId}");
using var cmdHandler = new CommandSubscriber();
cmdHandler.Subscribe<DanmuMsg>((msg) => Console.WriteLine($"[弹幕] {msg.UserName}: {msg.Msg}"));
cmdHandler.Subscribe<RoomRealTimeMessageUpdate>((msg) => Console.WriteLine($"直播间状态变更 粉丝数量: {msg.Fans}"));
cmdHandler.Subscribe<GuardBuy>((msg) => Console.WriteLine($"[上舰] {msg.UserName}"));
cmdHandler.Subscribe<InteractWord>((msg) => Console.WriteLine($"[进入] {msg.UserName} 进入直播间"));
cmdHandler.Subscribe<SuperChatMessage>((msg) => Console.WriteLine($"[SC] {msg.User.UserName} ({msg.Price}): {msg.Message}"));
cmdHandler.Subscribe<SendGift>((msg) => Console.WriteLine($"[礼物] {msg.SenderName} ({msg.CoinType} {msg.DiscountPrice}): {msg.Action}{msg.GiftName}"));
cmdHandler.Subscribe<AnchorLotStart>(msg => Console.WriteLine($"(天选开始) {msg.AwardName} 条件为{msg.RequireText} 发送弹幕{msg.Danmu}"));
cmdHandler.Subscribe<AnchorLotAward>(msg => Console.WriteLine($"(天选结束) {msg.AwardName} {string.Join(",", msg.AwardUsers.Select(u => u.UserName))}"));
cmdHandler.Subscribe<PopularityRedPocketStart>(msg => Console.WriteLine($"(红包开始) {msg.SenderName} 价值{msg.TotalPrice / 100}电池 发送弹幕{msg.Danmu}"));
cmdHandler.Subscribe<HotRankSettlementV2>(msg => Console.WriteLine($"(热门) {msg.Message}"));

await foreach (var @event in wsClient.Events(csc.Token))
{
    await cmdHandler.Handle(@event);
}
