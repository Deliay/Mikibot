# B站信息获取库
该项目主要用于直播弹幕获取，支持登陆到指定账号进行获取

## Websocket 弹幕抓取实现
见 `WebsocketCrawler` 文件夹的 `README`，可用事件可以在`ICommandBase`类实现中找到。

## API实现
| 类 | 用途
| - | - |
| BiliLiveCrawler | 直播弹幕、直播流相关API |
| BiliVideoCrawler | 视频相关信息API |

## 使用示例

### 获得直播间弹幕流
代码示例：
```csharp
var uid = 403496L;
var uidCookie = "...";
var roomId = 11306L;

// 用于获得登陆状态下观看直播的token，及弹幕服务器地址等等
var crawler = new BiliLiveCrawler();
crawler.SetCookie(uidCookie);

// 一些主播的直播房间号并不是真实房间号
// 需要调用B站API拿到真实房间号
var realRoomId = await crawler.GetRealRoomId(roomId, cancellationToken);

var liveToken = await crawler.GetLiveToken(realRoomId, cancellationToken);
var spectatorHost = liveToken.Hosts[0];

// 初始化wsClient实例
// 连接弹幕服务器，填入使用cookie获得的token
using var wsClient = new WebsocketClient();

// 可以不传crawler.Client，最好传一下，里面设置了Http Referer
// await wsClient.ConnectAsync(spectatorHost.Host, spectatorHost.WssPort, roomId, uid, token, "wss", cancellationToken);
await wsClient.ConnectAsync(crawler.Client, spectatorHost.Host, spectatorHost.WssPort, realRoomId, uid, token, "wss", cancellationToken);

// 获得事件
await foreach(var @event in wsClient.Events(cancellationToken))
{
    ...@event使用见下方处理事件示例
}
```

### 处理事件：使用CommandSubscriber
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

### 处理事件：手动处理
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
