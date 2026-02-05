# B站信息获取库
[![NuGet version](https://badge.fury.io/nu/Mikibot.Crawler.svg)](https://www.nuget.org/packages/Mikibot.Crawler)

该项目主要用于直播弹幕获取，支持登陆到指定账号进行获取


## Websocket 弹幕抓取实现
见 `WebsocketCrawler` 文件夹的 `README`，可用事件可以在`ICommandBase`类实现中找到。

## API实现
| 类                    | 用途            |
|----------------------|---------------|
| BiliLiveCrawler      | 直播弹幕、直播流相关API |
| BiliVideoCrawler     | 视频相关信息API     |
| BiliBasicInfoCrawler | 个人基本信息        |
| BiliVideoCrawler     | 视频信息          |

## 使用示例
`Program.cs` 中附带了一个可以运行的示例

### 举个例子
```csharp
using Mikibot.Crawler;
var serviceBuilder = new ServiceCollection();
serviceBuilder.AddBilibiliCrawlers();
serviceBuilder.AddTransient<WebsocketClient>();
await using var services = serviceBuilder.BuildServiceProvider();

var liveCrawler = services.GetRequiredService<BiliLiveCrawler>();
var account = services.GetRequiredService<BilibiliAccount>();

// 使用任意crawler设置cookie
liveCrawler.SetCookie("...");

// 初始化 account 及 wbi keys
var account = new BilibiliAccount(personalCrawler);
await account.InitializeAsync();

// 要抓的房间id
var uid = 403496L;
var uidCookie = "...";
var roomId = 11306L;

// 有些主播房间有靓号
var realRoomId = await crawler.GetRealRoomId(roomId, cancellationToken);

// 选择一个弹幕服务器
var spectatorHost = liveToken.Hosts[0];

// 连接弹幕服务器，填入使用cookie获得的token
var wsClient = services.GetRequiredService<WebsocketClient>();

// 拿到websocket认证需要的live token
var liveToken = await crawler.GetLiveToken(realRoomId, cancellationToken);

// 可以不传client，最好传一下，里面设置了Http Referer和Cookies
var client = services.GetRequiredService<HttpClient>();
await wsClient.ConnectAsync(client, spectatorHost.Host, spectatorHost.WssPort, realRoomId, uid, token, "wss", cancellationToken);

// 获得事件
await foreach(var @event in wsClient.Events(cancellationToken))
{
    ...@event使用见下方处理事件示例
}
```

#### 处理事件：使用CommandSubscriber
```csharp
// 事先准备好CommandSubscriber类
using var cmdHandler = new CommandSubscriber();
cmdHandler.Subscribe<DanmuMsg>((msg) => ...);
cmdHandler.Subscribe<DanmuMsg>(async (msg) => ...);
cmdHandler.Subscribe<SuperChatMessage>((msg) => ...);
cmdHandler.Subscribe<SendGift>(async (msg) => ...);

// 用CommandSubscriber处理直播事件
await commandHandler.Handle(@event);
```

#### 处理事件：手动处理
```csharp
// 或者手动处理直播事件

if (@event is Normal normalMessage)
{
    var cmd = ICommandBase.Parse(normal.RawContent);
   
    if (cmd is CommandBase<DanmuMsg> danmakuCmd)
    {
        // 处理弹幕消息
        var danmaku = danmakuCmd.Info;
    }
    else if (cmd is CommandBase<SendGift> giftCmd)
    {
        // 处理礼物消息
        var gift = giftCmd.Data;
    }
}
```
