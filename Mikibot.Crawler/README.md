# B站信息获取库
[![NuGet version](https://badge.fury.io/nu/Mikibot.Crawler.svg)](https://www.nuget.org/packages/Mikibot.Crawler)

该项目主要用于直播弹幕获取，支持登陆到指定账号进行获取


## Websocket 弹幕抓取实现
见 `WebsocketCrawler` 文件夹的 `README`，可用事件可以在`ICommandBase`类实现中找到。

## API实现
| 类                    | 用途             |
|----------------------|----------------|
| BiliLiveCrawler      | 直播弹幕、直播流相关API  |
| BiliVideoCrawler     | 视频相关信息API      |
| BiliBasicInfoCrawler | 个人基本信息         |
| BiliVideoCrawler     | 视频信息           |
| BilibiliAccount      | 个人基本信息和wbi key |

## 使用示例
`Program.cs` 中附带了一个可以运行的示例

### 举个例子
```csharp
using Mikibot.Crawler;

// setup DI container
var serviceBuilder = new ServiceCollection();
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

// handle events
await foreach(var @event in client.Events(cancellationToken))
{
    ...@event的使用见下方处理事件示例
}
```

#### 处理事件：使用CommandSubscriber
```csharp
// 事先准备好CommandSubscriber类，可以在继承了 IKnownCommand 的类中找到可用的事件
using var cmdHandler = new CommandSubscriber();
cmdHandler.Subscribe<DanmuMsg>((msg) => ...);
cmdHandler.Subscribe<DanmuMsg>(async (msg) => ...);
cmdHandler.Subscribe<SuperChatMessage>((msg) => ...);
cmdHandler.Subscribe<SendGift>(async (msg) => ...);

// foreach中用CommandSubscriber处理直播事件
await commandHandler.Handle(@event);
```

#### 处理事件：手动处理
```csharp
// 或者foreach中手动处理直播事件

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
