using Microsoft.Extensions.Logging;
using Mikibot.Analyze.MiraiHttp;
using Mikibot.Crawler.WebsocketCrawler.Data.Commands.KnownCommand;
using Mikibot.Crawler.WebsocketCrawler.Data.Commands.Utils;
using Mirai.Net.Data.Messages;
using Mirai.Net.Data.Messages.Concretes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mikibot.Analyze.Bot;

public static class MikiLiveEventProxyServiceExtensions
{
    public static void Register(this CommandSubscriber subscriber, MikiLiveEventProxyService danmakuCollector)
    {
        subscriber.Subscribe<AnchorLotStart>(danmakuCollector.HandleAnchorStart);
        subscriber.Subscribe<AnchorLotAward>(danmakuCollector.HandleAnchorAward);
        subscriber.Subscribe<PopularityRedPocketStart>(danmakuCollector.HandleRedPocketStart);
        subscriber.Subscribe<HotRankSettlementV2>(danmakuCollector.HandleHotRank);
    }
}

public class MikiLiveEventProxyService
{
    public MikiLiveEventProxyService(IMiraiService miraiService)
    {
        MiraiService = miraiService;
    }

    private IMiraiService MiraiService { get; }

    public async Task HandleAnchorStart(AnchorLotStart msg)
    {
        await MiraiService.SendMessageToAllGroup(default, new MessageBase[]
        {
            new PlainMessage() {
                Text = $"直播间开始抽奖 {msg.AwardName}，条件为 {msg.RequireText} 并发送弹幕 {msg.Danmu}",
            }
        });
    }

    public async Task HandleAnchorAward(AnchorLotAward msg)
    {
        var sb = new StringBuilder();
        sb.Append($"恭喜以下用户获得 {msg.AwardName}：\n");
        foreach (var user in msg.AwardUsers)
        {
            sb.Append($"{user.UserName} ({user.UserId})\n");
        }
        await MiraiService.SendMessageToAllGroup(default, new MessageBase[]
        {
            new PlainMessage() {
                Text = sb.ToString(),
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

    public async Task HandleHotRank(HotRankSettlementV2 msg)
    {
        await MiraiService.SendMessageToAllGroup(default, new MessageBase[]
        {
            new PlainMessage()
            {
                Text = $"{msg.Message}",
            }
        });
    }
}
