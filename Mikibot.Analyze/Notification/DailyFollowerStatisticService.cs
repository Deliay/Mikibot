using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mikibot.Crawler.Http.Bilibili;
using Mikibot.Database;
using Mikibot.Analyze.MiraiHttp;
using Mirai.Net.Data.Messages;
using Mirai.Net.Data.Messages.Concretes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mikibot.Analyze.Notification
{
    /// <summary>
    /// 每天新增关注数量统计
    /// </summary>
    public class DailyFollowerStatisticService
    {
        private readonly MikibotDatabaseContext db = new(MySqlConfiguration.FromEnviroment());

        public DailyFollowerStatisticService(BiliLiveCrawler crawler, IMiraiService mirai, ILogger<DailyFollowerStatisticService> logger)
        {
            Crawler = crawler;
            Mirai = mirai;
            Logger = logger;
        }

        public BiliLiveCrawler Crawler { get; }
        public IMiraiService Mirai { get; }
        public ILogger<DailyFollowerStatisticService> Logger { get; }

        public async Task IntervalCollectStatistics(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    // 每15秒收集一下数据
                    var count = await Crawler.GetFollowerCount(BiliLiveCrawler.mxmk, token);
                    await db.FollowerStatistic.AddAsync(new()
                    {
                        Bid = BiliLiveCrawler.mxmks,
                        CreatedAt = DateTimeOffset.Now,
                        FollowerCount = count,
                    }, token);
                    await db.SaveChangesAsync(token);
                    Logger.LogInformation("当前你弥粉丝数量 {}", count);
                    await Task.Delay(TimeSpan.FromSeconds(15), token);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "收集数据时发生了问题");
                }
            }
        }

        private async Task<int> GetRangeStartFollowerCount(DateTimeOffset min, CancellationToken token)
        {
            return (await db.FollowerStatistic
                    .Where(s => s.Bid == BiliLiveCrawler.mxmks)
                    .Where(s => s.CreatedAt > min)
                    .OrderBy(s => s.CreatedAt)
                    .FirstOrDefaultAsync(token))?.FollowerCount ?? 0;
        }
        private async Task<int> GetRangeEndFollowerCount(DateTimeOffset max, CancellationToken token)
        {
            return (await db.FollowerStatistic
                    .Where(s => s.Bid == BiliLiveCrawler.mxmks)
                    .OrderBy(s => s.CreatedAt)
                    .LastOrDefaultAsync(s => s.CreatedAt < max , token))?.FollowerCount ?? 0;
        }
        private async Task<int> GetRangeEndFollowerCount(DateTimeOffset max, DateTimeOffset min, CancellationToken token)
        {
            return (await db.FollowerStatistic
                    .Where(s => s.Bid == BiliLiveCrawler.mxmks)
                    .Where(s => s.CreatedAt > min)
                    .Where(s => s.CreatedAt < max)
                    .OrderByDescending(s => s.CreatedAt)
                    .FirstOrDefaultAsync(token))?.FollowerCount ?? 0;
        }

        private async Task<string> GetRecentlyLiveStreamStatus(DateTimeOffset start, CancellationToken token)
        {
            var result = await db.LiveStatuses
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

        public async Task DailyReport(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
#if !DEBUG
                while (DateTimeOffset.Now.Hour != 19)
                {
                    await Task.Delay(TimeSpan.FromMinutes(5), token);
                }
#endif
                try
                {
                    var end = DateTimeOffset.Now;
                    var start = end.Subtract(TimeSpan.FromDays(1));

                    var startFollowerCount = await GetRangeStartFollowerCount(start, token);
                    var endFollowerCount = await GetRangeEndFollowerCount(end, token);

                    var status = await GetRecentlyLiveStreamStatus(start, token);

                    var msg = $"涨粉日报\n{Format(start)} ~ {Format(end)}\n涨粉 {endFollowerCount - startFollowerCount} 人\n直播场次详细：\n{status}";
                    Logger.LogInformation("{}", msg);
                    await Mirai.SendMessageToAllGroup(token, new MessageBase[]
                    {
                        new PlainMessage(msg)
                    });
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "发送日报时出现了问题");
                }
                await Task.Delay(TimeSpan.FromHours(1), token);
            }
        }

        public async Task WeeklyReport(CancellationToken token)
        {
            var now = DateTimeOffset.Now;
            var end = now.Subtract(now.TimeOfDay);
            var start = end.Subtract(TimeSpan.FromDays(7));

            var startFollowerCount = await GetRangeStartFollowerCount(start, token);
            var endFollowerCount = await GetRangeEndFollowerCount(end, start, token);

            var increase = endFollowerCount - startFollowerCount;
            var sliverEstimate = (100000d - endFollowerCount) / increase;
            var estimateText = sliverEstimate > 0 ? $"按此涨粉速度,距离100000粉剩余{sliverEstimate:##.##}天" : "本周掉大粉咯！10万遥遥无期。";

            var status = await GetRecentlyLiveStreamStatus(start, token);
            var msg = $"涨粉周报\n{Format(start)} ~{Format(end)} 关注:{endFollowerCount}\n涨粉 {increase}人\n直播场次详细:\n{status}\n{estimateText}"; Logger.LogInformation("{}", msg);
            await Mirai.SendMessageToAllGroup(token, new MessageBase[]
            {
                new PlainMessage(msg)
            });
        }

        public async Task WeeklyReportSchedule(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
#if DEBUG
                    await WeeklyReport(token);
                    return;
#else
                    while (DateTimeOffset.Now.DayOfWeek != DayOfWeek.Monday)
                    {
                        await Task.Delay(TimeSpan.FromMinutes(30), token);
                    }
#endif
                    // 周一早 10:00AM 发
                    while (DateTimeOffset.Now.Hour != 10)
                    {
                        await Task.Delay(TimeSpan.FromMinutes(1), token);
                    }

                    await WeeklyReport(token);
                    await Task.Delay(TimeSpan.FromDays(1), token);
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "周报报错啦");
                }
            }


        }

        public Task SendAllReport(CancellationToken token)
        {
            return Task.WhenAll(DailyReport(token), WeeklyReportSchedule(token));
        }

        public async Task Run(CancellationToken token)
        {
            await Task.WhenAll(IntervalCollectStatistics(token), SendAllReport(token));
        }
    }
}
