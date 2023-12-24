using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mikibot.Crawler.Http.Bilibili;
using Mikibot.Crawler.Http.Bilibili.Model;
using Mikibot.Crawler.WebsocketCrawler.Client;
using Mikibot.Crawler.WebsocketCrawler.Data;
using Mikibot.Crawler.WebsocketCrawler.Data.Commands;
using Mikibot.Crawler.WebsocketCrawler.Data.Commands.KnownCommand;
using Mikibot.Crawler.WebsocketCrawler.Data.Commands.Utils;
using System.Text.Json;

var serviceBuilder = new ServiceCollection();
serviceBuilder.AddLogging(b => b.AddConsole());
serviceBuilder.AddSingleton<BiliLiveCrawler>();
serviceBuilder.AddSingleton<BiliVideoCrawler>();
serviceBuilder.AddTransient<WebsocketClient>();

using var services = serviceBuilder.BuildServiceProvider();
using var csc = new CancellationTokenSource();

var logger = services.GetRequiredService<ILogger<Program>>();
var crawler = services.GetRequiredService<BiliLiveCrawler>();
var wsClient = services.GetRequiredService<WebsocketClient>();
var roomId = 11306;

var playAddr = await crawler.GetLiveStreamAddressV2(roomId, csc.Token);

var personal = await crawler.GetLiveRoomInfo(roomId, csc.Token);
var realRoomId = await crawler.GetRealRoomId(roomId, csc.Token);
var spectatorEndpoint = await crawler.GetLiveToken(realRoomId, csc.Token);
var spectatorHost = spectatorEndpoint.Hosts[0];

var allGuards = new HashSet<GuardUserInfo>();
var init = await crawler.GetRoomGuardList(roomId, token: csc.Token);
allGuards.UnionWith(init.List);
allGuards.UnionWith(init.Top3);
while (init.List.Count > 0
    && init.Info.Count > allGuards.Count
    && init.Info.PageCount > init.Info.Current)
{
    init = await crawler.GetRoomGuardList(roomId, init.Info.Current + 1, csc.Token);
    allGuards.UnionWith(init.List);
}
var count = allGuards.Where(n => n.Online != 0).Count();
Console.WriteLine($"舰长在线数量:{count}");

await wsClient.ConnectAsync(spectatorHost.Host, spectatorHost.WssPort, realRoomId, 0, spectatorEndpoint.Token, "wss", cancellationToken: csc.Token);

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
