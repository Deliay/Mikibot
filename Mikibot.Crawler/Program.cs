
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mikibot.Crawler.Http.Bilibili;
using Mikibot.Crawler.Http.Bilibili.Model;
using Mikibot.Crawler.WebsocketCrawler.Client;
using Mikibot.Crawler.WebsocketCrawler.Data;
using Mikibot.Crawler.WebsocketCrawler.Data.Commands;
using Mikibot.Crawler.WebsocketCrawler.Data.Commands.KnownCommand;
using Mikibot.Crawler.WebsocketCrawler.Data.Commands.Utils;
using Mikibot.Crawler.WebsocketCrawler.Packet;
using System.Text.Json;

var serviceBuilder = new ServiceCollection();
serviceBuilder.AddLogging(b => b.AddConsole());
serviceBuilder.AddSingleton<BiliLiveCrawler>();
serviceBuilder.AddSingleton<BiliVideoCrawler>();
serviceBuilder.AddTransient<WebsocketClient>();

using var services = serviceBuilder.BuildServiceProvider();
using var csc = new CancellationTokenSource();

var vCrawler = services.GetRequiredService<BiliVideoCrawler>();

var rr = await vCrawler.GetVideoInfo("BV1kq4y1u7WL", null, csc.Token);
Console.WriteLine(JsonSerializer.Serialize(rr));
var logger = services.GetRequiredService<ILogger<Program>>();
var crawler = services.GetRequiredService<BiliLiveCrawler>();
var wsClient = services.GetRequiredService<WebsocketClient>();

var original = BasePacket.Auth(114514, "1919810");
var bytes = original.ToByte();
var restored = BasePacket.ToPacket(bytes);

var roomId = 1017;
var realRoomId = await crawler.GetRealRoomId(roomId, csc.Token);
var spectatorEndpoint = await crawler.GetLiveToken(realRoomId, csc.Token);
var spectatorHost = spectatorEndpoint.Hosts[0];

var allGuards = new HashSet<GuardUserInfo>();
var init = await crawler.GetRoomGuardList(21672023, token: csc.Token);
allGuards.UnionWith(init.List);
allGuards.UnionWith(init.Top3);
while (init.List.Count > 0
    && init.Info.Count > allGuards.Count
    && init.Info.PageCount > init.Info.Current)
{
    init = await crawler.GetRoomGuardList(21672023, init.Info.Current + 1, csc.Token);
    allGuards.UnionWith(init.List);
}
Console.WriteLine($"弥人舰长在线数量:{allGuards.Where(n => n.Online != 0).Count()}");

await wsClient.ConnectAsync(spectatorHost.Host, spectatorHost.WsPort, realRoomId, spectatorEndpoint.Token, cancellationToken: csc.Token);

var cmdHandler = new CommandSubscriber();
cmdHandler.Subscribe<DanmuMsg>((msg) => Console.WriteLine($"[弹幕] {msg.UserName}: {msg.Msg}"));
cmdHandler.Subscribe<RoomRealTimeMessageUpdate>((msg) => Console.WriteLine($"直播间状态变更 粉丝数量: {msg.Fans}"));
cmdHandler.Subscribe<GuardBuy>((msg) => Console.WriteLine($"[上舰] {msg.UserName}"));
cmdHandler.Subscribe<InteractWord>((msg) => Console.WriteLine($"[进入] {msg.UserName} 直播间"));
cmdHandler.Subscribe<SuperChatMessage>((msg) => Console.WriteLine($"[SC] {msg.User.UserName} ({msg.Price}): {msg.Message}"));
cmdHandler.Subscribe<SendGift>((msg) => Console.WriteLine($"[礼物] {msg.SenderName} ({msg.CoinType} {msg.DiscountPrice}): {msg.Action}{msg.GiftName}"));

await foreach (var @event in wsClient.Events(csc.Token))
{
    if (@event is Normal normal)
    {
        var cmd = ICommandBase.Parse(normal.RawContent);
        await cmdHandler.Handle(@event);
    }
}