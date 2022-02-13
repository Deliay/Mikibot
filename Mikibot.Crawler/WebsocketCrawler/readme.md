Mikibot B站直播间事件抓取模块
-----
.NET 6 下的Bilibili直播间弹幕抓取，看目前C#没啥好用的轮子，就造了个

网上资料也不全，调试起来心累 💔

## 使用方法
```C#
// 可以使用本项目的 BiliLiveCrawler 抓取一些前置需要的信息
// 也可以自行调用叔叔的API
var crawler = new BiliLiveCrawler();

// 一些主播的直播房间号并不是真实房间号
// 需要调用B站API拿到真实房间号
var uriRoomId = 21672023;
var realRoomId = await crawler.GetRealRoomId(roomId, cancellationToken);

// 调用B站API拿到直播间ws入口
var spectatorEndpoint = await crawler.GetLiveToken(realRoomId, cancellationToken);
var spectatorHost = spectatorEndpoint.Hosts[0];

// 初始化wsClient实例，并连接到直播间
var wsClient = new WebsocketClient();
await wsClient.ConnectAsync(spectatorHost.Host, spectatorHost.WsPort, realRoomId, cancellationToken: cancellationToken);

// 推荐使用CommandSubscriber来管理事件
var cmdHandler = new CommandSubscriber();
cmdHandler.Subscribe<DanmuMsg>((msg) => Console.WriteLine($"收到弹幕 {msg.UserName} {msg.Msg}"));
cmdHandler.Subscribe<RoomRealTimeMessageUpdate>((msg) => Console.WriteLine($"直播间状态变更 粉丝数量: {msg.Fans}"));
cmdHandler.Subscribe<GuardBuy>((msg) => Console.WriteLine($"{msg.UserName} 上舰了"));
cmdHandler.Subscribe<InteractWord>((msg) => Console.WriteLine($"{msg.UserName} 进入直播间"));

// 监听事件
await foreach (var @event in wsClient.Events(cancellationToken))
{
    // 使用`CommandSubscriber`来处理事件
    // 也可以手动处理
    await cmdHandler.Handle(@event);
}
```

## 名词解释
- **Command**: 直播间交互内容，内容为一个JSON
- **Packet**: 叔叔的二进制数据包

## 增加需要的Command事件(Normal包中的Command)
1. 增加`ICommandBase`中的Mapping关系（用于反序列化）
2. 增加`CommandSubscriber`中的Mapping关系（用于Subscribe）

## 增加需要的二进制包解析
1. 在`PacketType`中增加需要的包编号
2. 在`DataTypeMapping`中的`TypeMapping`增加对应实现规则即可


## 参考资料
https://github.com/SocialSisterYi/bilibili-API-collect/blob/master/live/message_stream.md