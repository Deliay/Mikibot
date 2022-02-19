using Microsoft.Extensions.Logging;
using Mikibot.AutoClipper.Abstract.Rquest;
using Mikibot.Crawler.Http.Bilibili;
using Mikibot.Crawler.WebsocketCrawler.Data.Commands.KnownCommand;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace Mikibot.Analyze.Notification
{
    public class DanmakuRecordControlService
    {
        private static readonly HttpClient httpClient = new();
        public DanmakuRecordControlService(BiliLiveCrawler crawler, ILogger<DanmakuRecordControlService> logger)
        {
            Crawler = crawler;
            Logger = logger;
        }

        public BiliLiveCrawler Crawler { get; }
        public ILogger<DanmakuRecordControlService> Logger { get; }
        public int RoomId { get; set; } = BiliLiveCrawler.mxmkr;
        private bool RecordingStatus { get; set; } = false;
        private SemaphoreSlim semaphore = new(1);

        private async Task<HttpResponseMessage> StartOrCancelRecording()
        {
            await semaphore.WaitAsync();
            try
            {
                if (!RecordingStatus)
                {
                    RecordingStatus = true;
                    return await httpClient.PostAsync($"http://localhost:19999/api/danmaku-record?Bid={RoomId}", null);
                }

                Logger.LogInformation("将在 {} 秒后结束自动切片", 10);
                await Task.Delay(TimeSpan.FromSeconds(10));
                RecordingStatus = false;
                return await httpClient.DeleteAsync($"http://localhost:19999/api/danmaku-record?Bid={RoomId}");
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

        public async Task HandleDanmu(DanmuMsg msg)
        {
            if (msg.UserId == 403496 && msg.Msg == "草！")
            {
                _ = StartOrCancelRecording();
            }
        }
    }
}
