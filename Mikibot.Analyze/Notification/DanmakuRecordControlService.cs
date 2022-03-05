using Microsoft.Extensions.Logging;
using Mikibot.Analyze.MiraiHttp;
using Mikibot.AutoClipper.Abstract.Rquest;
using Mikibot.BuildingBlocks.Util;
using Mikibot.Crawler.Http.Bilibili;
using Mikibot.Crawler.WebsocketCrawler.Data.Commands.KnownCommand;
using Mikibot.Database.Model;
using Mirai.Net.Data.Messages;
using Mirai.Net.Data.Messages.Concretes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Mikibot.Analyze.Notification
{
    public class DanmakuRecordControlService
    {
        private static readonly HttpClient httpClient = new()
        {
            Timeout = TimeSpan.FromMinutes(30),
        };
        public DanmakuRecordControlService(
            BiliLiveCrawler crawler,
            ILogger<DanmakuRecordControlService> logger,
            IMiraiService mirai,
            LiveStatusCrawlService liveStatus,
            OssService oss)
        {
            Crawler = crawler;
            Logger = logger;
            Mirai = mirai;
            LiveStatus = liveStatus;
            Oss = oss;
        }

        public BiliLiveCrawler Crawler { get; }
        public ILogger<DanmakuRecordControlService> Logger { get; }
        public IMiraiService Mirai { get; }
        public LiveStatusCrawlService LiveStatus { get; }
        public OssService Oss { get; }
        public int RoomId { get; set; } = BiliLiveCrawler.mxmkr;
        private bool RecordingStatus { get; set; } = false;
        private readonly SemaphoreSlim semaphore = new(1);

        private async Task InnerStartRecording()
        {
            // 检查当前直播状态
            var status = await LiveStatus.GetCurrentStatus(default);
            if (status.Status == 1)
            {
                RecordingStatus = true;
                await httpClient.PostAsync($"http://localhost:19990/api/danmaku-record?Bid={RoomId}", null);
            }
            else
            {
                Logger.LogWarning($"当前没有在直播，不能进行切片");
            }
        }

        private async Task InnerStopRecording()
        {
            Logger.LogInformation("将在 {} 秒后结束自动切片", 10);
            await Task.Delay(TimeSpan.FromSeconds(10));
            RecordingStatus = false;
            var result = await httpClient.DeleteAsync($"http://localhost:19990/api/danmaku-record?Bid={RoomId}");
            var record = await JsonSerializer.DeserializeAsync<LiveStreamRecord>(result.Content.ReadAsStream());
            if (record != null)
            {
                await Mirai.SendMessageToAllGroup(default, new MessageBase[]
                {
                    new PlainMessage($"刚才触发的切片: {record.LocalFileName} 已经完成，正在上传中...")
                });
                Logger.LogInformation("正在上传切片...");
                var downloadUrl = await Oss.Upload(record.LocalFileName);

                Logger.LogInformation("切片上传完成, 发送群通知中....{}", downloadUrl);
                await Mirai.SendMessageToSliceManGroup(default, new MessageBase[]
                {
                    new PlainMessage($"刚才触发的切片: {record.LocalFileName} 上传完成, 下载地址： {downloadUrl}")
                });
            }
        }

        private async Task StartRecording()
        {
            await semaphore.WaitAsync();
            try
            {
                if (!RecordingStatus)
                {
                    await InnerStartRecording();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An error was thrown when starting clip");
                throw;
            }
            finally
            {
                semaphore.Release();
            }
        }

        private async Task StopRecording()
        {
            try
            {
                if (RecordingStatus)
                {
                    await InnerStopRecording();
                }

            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An error was thrown when starting clip");
                throw;
            }
        }

        private readonly HashSet<int> AllowList = new()
        {
            403496,
            1829374,
            3542675,
            5152457,
            23034348,
        };

        public Task HandleDanmu(DanmuMsg msg)
        {
            if (AllowList.Contains(msg.UserId))
            {
                if (msg.Msg.EndsWith("！！！"))
                {
                    _ = StopRecording();
                }
                else if (msg.Msg.EndsWith("！！"))
                {
                    _ = StartRecording();
                }
            }

            return Task.CompletedTask;
        }
    }
}
