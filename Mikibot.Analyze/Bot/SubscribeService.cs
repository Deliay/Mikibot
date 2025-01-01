using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mikibot.Analyze.Generic;
using Mikibot.Analyze.MiraiHttp;
using Mikibot.Crawler.Http.Bilibili;
using Mikibot.Database;
using Mikibot.Database.Model;
using Mirai.Net.Data.Messages.Concretes;
using Mirai.Net.Data.Messages.Receivers;

namespace Mikibot.Analyze.Bot;

public partial class SubscribeService(
    IMiraiService miraiService,
    ILogger<SubscribeService> logger,
    BiliLiveCrawler crawler,
    PermissionService permissions,
    MikibotDatabaseContext db)
    : MiraiGroupMessageProcessor<SubscribeService>(miraiService, logger)
{
    private readonly IMiraiService _miraiService = miraiService;

    private async ValueTask SubscribeLive(SourceMessage? source,
        string groupId, string userId, bool isCancel,
        CancellationToken cancellationToken = default)
    {
        var alreadySubscribed = await db.SubscriptionLiveStarts
            .Where(s => s.GroupId == groupId && s.UserId == userId)
            .AnyAsync(cancellationToken);

        if (alreadySubscribed)
        {
            if (!isCancel) return;
            
            await db.SubscriptionLiveStarts.Where(s => s.GroupId == groupId && s.UserId == userId)
                .ExecuteDeleteAsync(cancellationToken);
            await _miraiService.SendMessageToSomeGroup([groupId], cancellationToken, 
                [
                    source!,
                    new PlainMessage($"已取消 群{groupId} 订阅主播 {userId} 的开播/下播提醒"),
                ]);
            return;
        }
        
        var roomInfo = await crawler.GetPersonalLiveRoomDetail(long.Parse(userId), cancellationToken);
        db.SubscriptionLiveStarts.Add(new SubscriptionLiveStart()
        {
            GroupId = groupId,
            UserId = userId,
            RoomId = roomInfo.RoomId.ToString(),
            EnabledFansTrendingStatistics = false,
        });
        await db.SaveChangesAsync(cancellationToken);
        await _miraiService.SendMessageToSomeGroup([groupId], cancellationToken,
            [
                source!,
                new PlainMessage($"已在 群{groupId} 订阅主播 {userId} (直播间{roomInfo.RoomId}) 的开播/下播提醒"),
            ]);
    }
    
    private async ValueTask SubscribeFans(SourceMessage? source,
        string groupId, string userId, bool isCancel,
        CancellationToken cancellationToken = default)
    {
        var alreadySubscribed = await db.SubscriptionFansTrends
            .Where(s => s.GroupId == groupId && s.UserId == userId)
            .AnyAsync(cancellationToken);

        if (alreadySubscribed)
        {
            if (!isCancel) return;
            
            await db.SubscriptionFansTrends.Where(s => s.GroupId == groupId && s.UserId == userId)
                .ExecuteDeleteAsync(cancellationToken);
            await _miraiService.SendMessageToSomeGroup([groupId], cancellationToken, 
                [
                    source!,
                    new PlainMessage($"已取消 群{groupId} 订阅主播 {userId} 的涨粉日报/周报")
                ]);
            return;
        }
        
        var roomInfo = await crawler.GetPersonalLiveRoomDetail(long.Parse(userId), cancellationToken);
        db.SubscriptionFansTrends.Add(new SubscriptionFansTrends()
        {
            GroupId = groupId,
            UserId = userId,
            TargetFansCount = 100_0000,
        });
        await db.SaveChangesAsync(cancellationToken);
        await _miraiService.SendMessageToSomeGroup([groupId], cancellationToken, 
            [
                source!,
                new PlainMessage($"已在 群{groupId} 订阅主播 {userId} 的涨粉日报/周报"),
            ]);
    }
    
    [GeneratedRegex(@"(!|！)\s*(取消)?订阅(涨粉|开播)(\d+)$")]
    private static partial Regex GenerateBindMatchRegex();

    private static readonly Regex BindMatchRegex = GenerateBindMatchRegex();

    protected override async ValueTask Process(GroupMessageReceiver message, CancellationToken token = default)
    {
        var source = message.MessageChain.GetSourceMessage();
        foreach (var msg in message.MessageChain)
        {
            if (msg is not PlainMessage plain) continue;
            
            var matches = BindMatchRegex.Matches(plain.Text);
            if (matches.Count == 0) return;

            if (!await permissions.IsBotOperator(message.Sender.Id, token))
            {
                return;
            }
            
            var (isCancel, type, bid) = matches
                .Select(m => (m.Groups[2].Value == "取消", m.Groups[3].Value, m.Groups[4].Value))
                .FirstOrDefault();

            if (type is not { Length: > 0 } || bid is not { Length: > 0 }) return;
            
            await (type switch
            {
                "涨粉" => SubscribeFans(source, message.GroupId, bid, isCancel, token),
                "开播" => SubscribeLive(source, message.GroupId, bid, isCancel, token),
                _ => ValueTask.CompletedTask
            });
            return;
        }
    }
}