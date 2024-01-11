using Mikibot.Analyze.MiraiHttp;
using Mikibot.Crawler.WebsocketCrawler.Data.Commands.KnownCommand;
using Mikibot.Crawler.WebsocketCrawler.Data.Commands.Utils;
using Mirai.Net.Data.Messages;
using Mirai.Net.Data.Messages.Concretes;
using System.Text;

namespace Mikibot.Analyze.Bot;

public static class MikiLiveEventProxyServiceExtensions
{
    public static void Register(this CommandSubscriber subscriber, MikiLiveEventProxyService danmakuCollector)
    {
        subscriber.Subscribe<AnchorLotStart>(danmakuCollector.HandleAnchorStart);
        subscriber.Subscribe<AnchorLotAward>(danmakuCollector.HandleAnchorAward);
        subscriber.Subscribe<PopularityRedPocketStart>(danmakuCollector.HandleRedPocketStart);
    }
}

public class MikiLiveEventProxyService(IMiraiService miraiService)
{
    private IMiraiService MiraiService { get; } = miraiService;

    public async Task HandleAnchorStart(AnchorLotStart msg)
    {
        await MiraiService.SendMessageToAllGroup(default, new MessageBase[]
        {
            new PlainMessage() {
                Text = $"阿弥弥开天选啦~ 抽 {msg.AwardName}",
            }
        });
    }

    public async Task HandleAnchorAward(AnchorLotAward msg)
    {
        var title = $"{msg.AwardName} 中奖名单";
        var userList = string.Join('\n', msg.AwardUsers.Select((user) => $"{user.UserName} ({user.UserId})"));

        await MiraiService.SendMessageToAllGroup(default, new MessageBase[]
        {
            new PlainMessage() {
                Text = $"{string.Join('\n', title, userList)}",
            }
        });
    }

    public async Task HandleRedPocketStart(PopularityRedPocketStart msg)
    {
        await MiraiService.SendMessageToAllGroup(default, new MessageBase[]
        {
            new PlainMessage()
            {
                Text = $"{msg.SenderName}在直播间发放了红包，价值{msg.TotalPrice / 100}电池",
            }
        });
    }
}
