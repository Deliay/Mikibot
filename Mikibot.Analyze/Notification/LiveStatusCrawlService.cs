using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mikibot.Database;
using Mikibot.Database.Model;
using Mikibot.Analyze.MiraiHttp;
using Mikibot.Crawler.Http.Bilibili;
using Mikibot.Crawler.Http.Bilibili.Model;
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
    /// 开播、下播通知
    /// </summary>
    public class LiveStatusCrawlService
    {
        private readonly MikibotDatabaseContext db = new (MySqlConfiguration.FromEnviroment());

        public LiveStatusCrawlService(BiliLiveCrawler crawler, IMiraiService mirai, ILogger<LiveStatusCrawlService> logger)
        {
            Crawler = crawler;
            Mirai = mirai;
            Logger = logger;
        }

        public BiliLiveCrawler Crawler { get; }
        public IMiraiService Mirai { get; }
        public ILogger<LiveStatusCrawlService> Logger { get; }

        public async Task<LiveStatus> GetCurrentStatus(string roomId, CancellationToken token)
        {
            var bid = GetBid(roomId);
            return await db.LiveStatuses.Where(s => s.Bid == bid).OrderBy(s => s.Id).LastOrDefaultAsync(token);
        }

        private readonly Random random = new();

        private static readonly Dictionary<string, List<string>> GroupMapping = new()
        {
            { $"{BiliLiveCrawler.mxmkr}", ["314503649", "139528984"] },
            { $"{22323445}", ["650042418"] },
        };

        private const string AkumariaRid = "22323445";
        private const string AkumariaBid = "576858552";
        private const string MxmkRid = "21672023";

        private static string GetBid(string roomId) => roomId switch {
            AkumariaRid => AkumariaBid,
            MxmkRid => BiliLiveCrawler.mxmks,
            _ => throw new InvalidOperationException()
        };

        private async ValueTask<LiveStatus> GenerateStatus(string bid, LiveRoomInfo info, CancellationToken token)
            => new()
            {
                Cover = info.Background,
                Notified = false,
                Status = info.LiveStatus,
                FollowerCount = (int)await Crawler.GetFollowerCount(BiliLiveCrawler.mxmk, token),
                StatusChangedAt = DateTimeOffset.Now,
                UpdatedAt = DateTimeOffset.Now,
                Title = info.Title,
                Bid = bid,
            };

        private async ValueTask InsertStatus(LiveStatus status, CancellationToken token)
        {
            Logger.LogInformation("准备写入状态:{}, 标题={}", status.Status, status.Title);
            await db.LiveStatuses.AddAsync(status, token);
            await db.SaveChangesAsync(token);
        }

        private MessageBase[] ComposeMessage(LiveRoomInfo info, LiveStatus? latest, LiveStatus newly)
        {
            string status() => info.LiveStatus == 1 ? "开" : "下";
            string fans() => newly.Status == 0 && latest != null ? $"本次直播涨粉: {newly.FollowerCount - latest.FollowerCount}" : "";
            string url() => newly.Status == 1 ? $"https://live.bilibili.com/{info.RoomId}" : "";
            var msg = $"{status()}啦~ {info.Title}\n{url()}{fans()}";
            Logger.LogInformation("Message composed {}", msg);
            return
            [
                new ImageMessage() { Url = info.UserCover },
                new PlainMessage(msg),
            ];
        }

        public async Task Run(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                var next = random.Next(15, 30);
                Logger.LogInformation("{} 秒后开始同步状态", next);
                // 每15~30秒收集一次数据
                await Task.Delay(TimeSpan.FromSeconds(next), token);
                Logger.LogInformation("开始同步状态");
                try
                {
                    var latest = await db.LiveStatuses.OrderBy(s => s.Id).LastOrDefaultAsync(token);
                    var info = await Crawler.GetLiveRoomInfo(BiliLiveCrawler.mxmkr, token);
                    // 发通知咯！
                    if (latest == null || (latest.Status != info.LiveStatus))
                    {
                        // 将最新数据入库
                        var newly = await GenerateStatus(info, token);
                        await InsertStatus(newly, token);
                        Logger.LogInformation("写入数据库完成");
                        // 发开播消息
                        await Mirai.SendMessageToAllGroup(token, ComposeMessage(info, latest, newly));
                        Logger.LogInformation("同步QQ消息完成");
                    }
                    else
                    {
                        Logger.LogInformation("直播状态没有发生变化");
                    }
                    Logger.LogInformation("状态同步完成");
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "状态同步失败");
                }
            }
        }
    }
}
