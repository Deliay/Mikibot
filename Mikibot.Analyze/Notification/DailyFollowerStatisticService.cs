using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mikibot.Crawler.Http.Bilibili;
using Mikibot.Database;
using Mikibot.Analyze.MiraiHttp;
using Mirai.Net.Data.Messages;
using Mirai.Net.Data.Messages.Concretes;
using Mikibot.Database.Model;
using Mirai.Net.Data.Shared;

namespace Mikibot.Analyze.Notification;

/// <summary>
/// 每天新增关注数量统计
/// </summary>
public class DailyFollowerStatisticService(IQqService qq, ILogger<DailyFollowerStatisticService> logger)
{
    private readonly MikibotDatabaseContext db = new(MySqlConfiguration.FromEnviroment());

    // 不需要登录

    private BiliLiveCrawler Crawler { get; } = new(new HttpClient());
    private IQqService Qq { get; } = qq;
    private ILogger<DailyFollowerStatisticService> Logger { get; } = logger;
    private readonly Random random = new();

    private async Task IntervalCollectStatistics(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            var next = random.Next(15, 30);
            Logger.LogInformation("{} 秒后开始统计的粉丝数量", next);
            // 每15~30秒收集一下数据
            await Task.Delay(TimeSpan.FromSeconds(next), token);
            var allSubscriptions = await db.SubscriptionFansTrends.ToListAsync(token);
            var distinctUserIds = allSubscriptions.Select(s => s.UserId).Distinct();
            foreach (var strUserId in distinctUserIds)
            {
                try
                {
                    // wait some random periodic to avoid bilibili ban our ips
                    await Task.Delay(TimeSpan.FromSeconds(random.Next(1, 3)), token);
                        
                    var userId = long.Parse(strUserId);
                    var count = await Crawler.GetFollowerCount(userId, token);
                    await db.FollowerStatistic.AddAsync(new FollowerStatistic
                    {
                        Bid = strUserId,
                        CreatedAt = DateTimeOffset.Now,
                        FollowerCount = (int)count,
                    }, token);
                    await db.SaveChangesAsync(token);
                    Logger.LogInformation("当前你弥粉丝数量 {}", count);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "收集数据时发生了问题");
                }
            }
        }
    }

    private async Task<int> GetRangeStartFollowerCount(string userId, DateTimeOffset min, CancellationToken token)
    {
        return (await db.FollowerStatistic
            .Where(s => s.Bid == userId)
            .Where(s => s.CreatedAt > min)
            .OrderBy(s => s.CreatedAt)
            .FirstOrDefaultAsync(token))?.FollowerCount ?? 0;
    }
    private async Task<int> GetRangeEndFollowerCount(string userId, DateTimeOffset max, CancellationToken token)
    {
        return (await db.FollowerStatistic
            .Where(s => s.Bid == userId)
            .OrderBy(s => s.CreatedAt)
            .LastOrDefaultAsync(s => s.CreatedAt < max , token))?.FollowerCount ?? 0;
    }
    private async Task<int> GetRangeEndFollowerCount(string userId, DateTimeOffset max, DateTimeOffset min, CancellationToken token)
    {
        return (await db.FollowerStatistic
            .Where(s => s.Bid == userId)
            .Where(s => s.CreatedAt > min)
            .Where(s => s.CreatedAt < max)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync(token))?.FollowerCount ?? 0;
    }

    private async Task<string> GetRecentlyLiveStreamStatus(string userId, DateTimeOffset start, CancellationToken token)
    {
        var result = await db.LiveStatuses
            .Where(s => s.Bid == userId)
            .Where(s => s.CreatedAt > start)
            .GroupBy(s => s.Title)
            .Select(sg => new
            {
                Count = sg.Max(s => s.FollowerCount) - sg.Min(s => s.FollowerCount),
                Title = sg.Key,
            }).ToListAsync(token);
        return string.Join('\n', result.Select(c => $"- {c.Title}, 涨粉: {c.Count}"));
    }

    private static string Format(DateTimeOffset date)
    {
        return date.ToString("yyyy-MM-dd HH:mm");
    }

    private async Task DailyReportAll(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            while (DateTimeOffset.Now.Hour != 19 || DateTimeOffset.Now.Minute > 15 )
            {
                await Task.Delay(TimeSpan.FromMinutes(5), token);
            }

            var subscriptions = await db.SubscriptionFansTrends.ToListAsync(token);

            foreach (var subscription in subscriptions)
            {
                await DailyReport(subscription, token);
            }
        }
    }
        
    private async Task DailyReport(SubscriptionFansTrends subscription, CancellationToken token)
    {
        try
        {
            var userId = subscription.UserId;
                
            var end = DateTimeOffset.Now;
            var start = end.Subtract(TimeSpan.FromDays(1));

            var startFollowerCount = await GetRangeStartFollowerCount(userId, start, token);
            var endFollowerCount = await GetRangeEndFollowerCount(userId, end, token);

            var status = await GetRecentlyLiveStreamStatus(userId, start, token);

            var msg = $"涨粉日报\n{Format(start)} ~ {Format(end)}\n涨粉 {endFollowerCount - startFollowerCount} 人\n直播场次详细：\n{status}";
            Logger.LogInformation("{}", msg);
            await Qq.SendMessageToSomeGroup([subscription.GroupId], token,
                new PlainMessage(msg));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "发送日报时出现了问题");
        }
        await Task.Delay(TimeSpan.FromHours(1), token);
    }

    public async Task WeeklyReport(SubscriptionFansTrends subscription, CancellationToken token)
    {
        var userId = subscription.UserId;
        var targetCount = subscription.TargetFansCount;
            
        var now = DateTimeOffset.Now;
        var end = now.Subtract(now.TimeOfDay);
        var start = end.Subtract(TimeSpan.FromDays(7));

        var startFollowerCount = await GetRangeStartFollowerCount(userId, start, token);
        var endFollowerCount = await GetRangeEndFollowerCount(userId, end, start, token);

        var increase = endFollowerCount - startFollowerCount;
            
        targetCount = Math.Max(targetCount, endFollowerCount + 1);
            
        var sliverEstimate = (targetCount - endFollowerCount) / increase * 7;
        var estimateText = sliverEstimate > 0? $"按此涨粉速度,距离{targetCount}粉剩余{sliverEstimate:##.##}天" : "本周掉大粉咯！20万遥遥无期。";

        var status = await GetRecentlyLiveStreamStatus(userId, start, token);
        var msg = $"涨粉周报\n{Format(start)} ~{Format(end)} 关注:{endFollowerCount}\n涨粉 {increase}人\n直播场次详细:\n{status}\n{estimateText}"; Logger.LogInformation("{}", msg);
        await Qq.SendMessageToSomeGroup([subscription.GroupId], token, new PlainMessage(msg));
    }

    public async Task WeeklyReportSchedule(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                while (DateTimeOffset.Now.DayOfWeek != DayOfWeek.Monday)
                {
                    await Task.Delay(TimeSpan.FromMinutes(30), token);
                }
                    
                // 周一早 10:00AM 发
                while (DateTimeOffset.Now.Hour != 10)
                {
                    await Task.Delay(TimeSpan.FromMinutes(1), token);
                }

                var subscriptions = await db.SubscriptionFansTrends.ToListAsync(token);
                    
                foreach (var subscription in subscriptions)
                {
                    await WeeklyReport(subscription, token);
                }
                await Task.Delay(TimeSpan.FromDays(1), token);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "周报报错啦");
            }
        }


    }

    private Task SendAllReport(CancellationToken token)
    {
        return Task.WhenAll(DailyReportAll(token), WeeklyReportSchedule(token));
    }

    public async Task Run(CancellationToken token)
    {
        await Task.WhenAll(IntervalCollectStatistics(token), SendAllReport(token));
    }
}