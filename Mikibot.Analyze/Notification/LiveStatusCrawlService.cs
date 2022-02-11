using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mikibot.Analyze.Database;
using Mikibot.Analyze.Database.Model;
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

        private async ValueTask<LiveStatus> GenerateStatus(PersonalInfo.LiveRoomDetail info, CancellationToken token)
            => new()
            {
                Cover = info.Cover,
                Notified = false,
                Status = info.LiveStatus,
                FollowerCount = await Crawler.GetFollowerCount(BiliLiveCrawler.mxmk, token),
                StatusChangedAt = DateTimeOffset.Now,
                UpdatedAt = DateTimeOffset.Now,
                Title = info.Title,
                Bid = $"{BiliLiveCrawler.mxmk}",
            };

        private async ValueTask InsertStatus(LiveStatus status, CancellationToken token)
        {
            Logger.LogInformation("准备写入状态:{}, 标题={}", status.Status, status.Title);
            await db.LiveStatuses.AddAsync(status, token);
            await db.SaveChangesAsync(token);
        }

        private MessageBase[] ComposeMessage(PersonalInfo.LiveRoomDetail info, LiveStatus? latest, LiveStatus newly)
        {
            string status() => info.LiveStatus == 1 ? "开" : "下";
            string fans() => newly.Status == 0 && latest != null ? $"本次直播涨粉: {newly.FollowerCount - latest.FollowerCount}" : "";
            string url() => newly.Status == 1 ? info.Url : "";
            var msg = $"{status()}啦~ {info.Title}\n{url()}{fans()}";
            Logger.LogInformation("Message composed {}", msg);
            return new MessageBase[]
            {
                new ImageMessage() { Url = info.Cover },
                new PlainMessage(msg),
            };
        }

        public async Task Run(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                Logger.LogInformation("开始同步状态");
                try
                {
                    var latest = await db.LiveStatuses.OrderBy(s => s.Id).LastOrDefaultAsync(token);
                    var info = (await Crawler.GetPersonalInfo(BiliLiveCrawler.mxmk, token)).LiveRoom;
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
                await Task.Delay(TimeSpan.FromSeconds(15), token);
            }
        }
    }
}
