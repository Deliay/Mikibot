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
                    .OrderBy(s => s.CreatedAt)
                    .FirstOrDefaultAsync(s => s.CreatedAt > min, token))?.FollowerCount ?? 0;
        }
        private async Task<int> GetRangeEndFollowerCount(DateTimeOffset max, CancellationToken token)
        {
            return (await db.FollowerStatistic
                    .Where(s => s.Bid == BiliLiveCrawler.mxmks)
                    .OrderBy(s => s.CreatedAt)
                    .LastOrDefaultAsync(s => s.CreatedAt < max , token))?.FollowerCount ?? 0;
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

        public async Task Run(CancellationToken token)
        {
            await Task.WhenAll(IntervalCollectStatistics(token), DailyReport(token));
        }
    }
}
