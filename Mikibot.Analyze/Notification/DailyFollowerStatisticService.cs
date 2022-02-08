using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mikibot.Mirai.Crawler.Bilibili;
using Mikibot.Mirai.Database;
using Mikibot.Mirai.MiraiHttp;
using Mirai.Net.Data.Messages;
using Mirai.Net.Data.Messages.Concretes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mikibot.Mirai.Notification
{
    /// <summary>
    /// 每天新增关注数量统计
    /// </summary>
    public class DailyFollowerStatisticService
    {
        private readonly MikibotDatabaseContext db = new(MySqlConfiguration.FromEnviroment());

        public DailyFollowerStatisticService(BilibiliCrawler crawler, MiraiService mirai, ILogger<DailyFollowerStatisticService> logger)
        {
            Crawler = crawler;
            Mirai = mirai;
            Logger = logger;
        }

        public BilibiliCrawler Crawler { get; }
        public MiraiService Mirai { get; }
        public ILogger<DailyFollowerStatisticService> Logger { get; }

        public async Task IntervalCollectStatistics(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    // 每15秒收集一下数据
                    var count = await Crawler.GetFollowerCount(BilibiliCrawler.mxmk, token);
                    await db.FollowerStatistic.AddAsync(new()
                    {
                        Bid = BilibiliCrawler.mxmks,
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
                    .Where(s => s.Bid == BilibiliCrawler.mxmks)
                    .OrderBy(s => s.CreatedAt)
                    .FirstOrDefaultAsync(s => s.CreatedAt > min, token))?.FollowerCount ?? 0;
        }
        private async Task<int> GetRangeEndFollowerCount(DateTimeOffset max, CancellationToken token)
        {
            return (await db.FollowerStatistic
                    .Where(s => s.Bid == BilibiliCrawler.mxmks)
                    .OrderBy(s => s.CreatedAt)
                    .LastOrDefaultAsync(s => s.CreatedAt < max , token))?.FollowerCount ?? 0;
        }

        public async Task DailyReport(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                while (DateTimeOffset.Now.Hour != 19)
                {
                    await Task.Delay(TimeSpan.FromMinutes(5), token);
                }
                try
                {
                    var end = DateTimeOffset.Now;
                    var start = end.Subtract(TimeSpan.FromDays(1));

                    var startFollowerCount = await GetRangeStartFollowerCount(start, token);
                    var endFollowerCount = await GetRangeEndFollowerCount(end, token);

                    var msg = $"从 {start} 到 {end} 你弥共涨粉 {endFollowerCount - startFollowerCount} 人";
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
