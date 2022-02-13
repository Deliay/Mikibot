
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mikibot.Crawler.Http.Bilibili;
using Mikibot.Crawler.WebsocketCrawler.Client;
using Mikibot.Crawler.WebsocketCrawler.Data.Commands;
using Mikibot.Crawler.WebsocketCrawler.Data.Commands.Utils;
using Mikibot.Crawler.WebsocketCrawler.Packet;

var serviceBuilder = new ServiceCollection();
serviceBuilder.AddLogging(b => b.AddConsole());
serviceBuilder.AddSingleton<BiliLiveCrawler>();
serviceBuilder.AddTransient<WebsocketClient>();

using var services = serviceBuilder.BuildServiceProvider();
using var csc = new CancellationTokenSource();

var logger = services.GetRequiredService<ILogger<Program>>();
var crawler = services.GetRequiredService<BiliLiveCrawler>();
var wsClient = services.GetRequiredService<WebsocketClient>();

var original = BasePacket.Auth(114514, "1919810");
var bytes = original.ToByte();
var restored = BasePacket.ToPacket(bytes);

var roomId = 21672023;
var realRoomId = await crawler.GetRealRoomId(roomId, csc.Token);
var spectatorEndpoint = await crawler.GetLiveToken(realRoomId, csc.Token);
var spectatorHost = spectatorEndpoint.Hosts[0];

await wsClient.ConnectAsync(spectatorHost.Host, spectatorHost.WsPort, realRoomId, spectatorEndpoint.Token, cancellationToken: csc.Token);

var cmdHandler = new CommandSubscriber();
cmdHandler.Subscribe<DanmuMsg>((msg) => Console.WriteLine($"收到弹幕 {msg.UserName} {msg.Msg}"));
cmdHandler.Subscribe<RoomRealTimeMessageUpdate>((msg) => Console.WriteLine($"直播间状态变更 粉丝数量: {msg.Fans}"));
cmdHandler.Subscribe<GuardBuy>((msg) => Console.WriteLine($"{msg.UserName} 上舰了"));
cmdHandler.Subscribe<InteractWord>((msg) => Console.WriteLine($"{msg.UserName} 进入直播间"));

await foreach (var @event in wsClient.Events(csc.Token))
{
    await cmdHandler.Handle(@event);
}