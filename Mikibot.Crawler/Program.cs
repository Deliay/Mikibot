using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mikibot.Crawler;
using Mikibot.Crawler.Http;
using Mikibot.Crawler.Http.Bilibili;
using Mikibot.Crawler.WebsocketCrawler.Client;
using Mikibot.Crawler.WebsocketCrawler.Data.Commands.KnownCommand;
using Mikibot.Crawler.WebsocketCrawler.Data.Commands.KnownCommand.ProtoCommand;
using Mikibot.Crawler.WebsocketCrawler.Data.Commands.Utils;

var serviceBuilder = new ServiceCollection();
serviceBuilder.AddLogging(b => b.AddConsole());
var cookie = "设置cookie";
serviceBuilder.AddSingleton(new CookieJar(cookie));
serviceBuilder.AddBilibiliCrawlers();

await using var services = serviceBuilder.BuildServiceProvider();
using var csc = new CancellationTokenSource();
var cancellationToken = csc.Token;

// initialize wbi keys
var account = services.GetRequiredService<BilibiliAccount>();
await account.InitializeAsync(cancellationToken);

// connect to danmaku server
var client = services.GetRequiredService<DanmakuClient>();
var roomId = 11306;
await client.ConnectAsync(roomId, cancellationToken);

Console.WriteLine($"已连接到 {roomId}");

// prepare subscribers
using var cmdHandler = new CommandSubscriber();
cmdHandler.Subscribe<DanmuMsg>((msg) => Console.WriteLine($"[弹幕] {msg.UserName}: {msg.Msg}"));
cmdHandler.Subscribe<RoomRealTimeMessageUpdate>((msg) => Console.WriteLine($"直播间状态变更 粉丝数量: {msg.Fans}"));
cmdHandler.Subscribe<GuardBuy>((msg) => Console.WriteLine($"[上舰] {msg.UserName}"));
cmdHandler.Subscribe<InteractWord>((msg) => Console.WriteLine($"[进入] {msg.UserName} 进入直播间"));
cmdHandler.Subscribe<InteractWordV2>((msg) =>
{
    var data = msg.Parse();
    Console.WriteLine($"[进入] {data.Name} 进入直播间");
});
cmdHandler.Subscribe<SuperChatMessage>((msg) => Console.WriteLine($"[SC] {msg.User.UserName} ({msg.Price}): {msg.Message}"));
cmdHandler.Subscribe<SendGift>((msg) => Console.WriteLine($"[礼物] {msg.SenderName} ({msg.CoinType} {msg.DiscountPrice}): {msg.Action}{msg.GiftName}"));
cmdHandler.Subscribe<AnchorLotStart>(msg => Console.WriteLine($"(天选开始) {msg.AwardName} 条件为{msg.RequireText} 发送弹幕{msg.Danmu}"));
cmdHandler.Subscribe<AnchorLotAward>(msg => Console.WriteLine($"(天选结束) {msg.AwardName} {string.Join(",", msg.AwardUsers.Select(u => u.UserName))}"));
cmdHandler.Subscribe<PopularityRedPocketStart>(msg => Console.WriteLine($"(红包开始) {msg.SenderName} 价值{msg.TotalPrice / 100}电池 发送弹幕{msg.Danmu}"));
cmdHandler.Subscribe<HotRankSettlementV2>(msg => Console.WriteLine($"(热门) {msg.Message}"));
cmdHandler.Subscribe<WatchedChange>(msg => Console.WriteLine($"[观看] {msg.Count} 人看过"));

// handle events
await foreach (var @event in client.Events(cancellationToken))
{
    await cmdHandler.Handle(@event);
}
