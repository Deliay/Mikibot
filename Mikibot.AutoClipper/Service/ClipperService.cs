using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mikibot.Crawler.Http.Bilibili;
using Mikibot.Database;
using Mikibot.Database.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mikibot.AutoClipper.Service
{
    public class ClipperService
    {
        public ClipperService(BiliLiveCrawler crawler, ILogger<ClipperService> logger, MikibotDatabaseContext db)
        {
            Crawler = crawler;
            Logger = logger;
            Db = db;
        }

        private static readonly HttpClient HttpClient = new();
        private const string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/98.0.4758.80 Safari/537.36 Edg/98.0.1108.50";
        static ClipperService()
        {
            HttpClient.DefaultRequestHeaders.Add("User-Agent", UserAgent);
        }
        public BiliLiveCrawler Crawler { get; }
        public ILogger<ClipperService> Logger { get; }
        public MikibotDatabaseContext Db { get; }

        private readonly Dictionary<int, CancellationTokenSource> _danmakuController = new();
        private readonly Dictionary<int, Task<LiveStreamRecord>> _danmakuTask = new();
        private readonly Dictionary<int, CancellationTokenSource> _taskController = new();
        private readonly Dictionary<int, Task> _tasks = new();
        private readonly SemaphoreSlim _semaphore = new(1);

        /// <summary>
        /// 移除 n * 3 分钟之前的切片（只保留最近3个切片）
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task RemoveOldClips(TimeSpan duration, CancellationToken cancellationToken)
        {
            var reserveTime = DateTimeOffset.Now.Subtract(duration * 3 + TimeSpan.FromSeconds(1));
            var oldClips = await Db.LiveStreamRecords
                .Where(rec => rec.RecordStoppedAt <= reserveTime)
                .Where(rec => !rec.Reserve)
                .ToListAsync(cancellationToken);

            foreach (var rec in oldClips)
            {
                try
                {
                    Logger.LogInformation("Trying remove old clips: #{} - {}", rec.Id, rec.LocalFileName);
                    File.Delete(rec.LocalFileName);
                    Db.Remove(rec);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error thrown when clean old clips");
                }
            }
            await Db.SaveChangesAsync(cancellationToken);
        }

        private string GetClipFileName(int roomId)
        {
            var path = Environment.GetEnvironmentVariable("MIKIBOT_AUTO_CLIP_PATH") ?? Path.GetTempPath();
            if (!Directory.Exists(path)) path = Path.GetTempPath();

            var file = $"clip-{roomId}-{DateTimeOffset.Now:yyyy-MM-dd-HH-mm-ss}.flv";

            return Path.Combine(path, file);
        }

        /// <summary>
        /// 进行指定时间的切片
        /// </summary>
        /// <param name="roomId"></param>
        /// <param name="url"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<LiveStreamRecord> PeriodicClip(int roomId, string url, TimeSpan duration, CancellationTokenSource cts, bool reserve = false)
        {
            var token = cts.Token;
            
            var clipRecord = new LiveStreamRecord()
            {
                LocalFileName = GetClipFileName(roomId),
                Bid = roomId,
                CreatedAt = DateTimeOffset.Now,
                Reserve = reserve,
            };
            await Db.AddAsync(clipRecord, token);
            await Db.SaveChangesAsync(token);

            Logger.LogInformation("Clip will save to {}", clipRecord.LocalFileName);
            using var fileStream = File.OpenWrite(clipRecord.LocalFileName);

            var startedAt = DateTimeOffset.Now;
            cts.CancelAfter(duration);

            try
            {
                while (!cts.IsCancellationRequested)
                {
                    Logger.LogInformation("Request live stream from {}", url);
                    using var res = await HttpClient.GetStreamAsync(url, token);
                    await res.CopyToAsync(fileStream, token);
                    await fileStream.FlushAsync(token);
                }
            }
            catch (Exception ex) when (ex is TaskCanceledException || ex is OperationCanceledException)
            {
                Logger.LogInformation("Clip cancelled");
            }
            var endedAt = DateTimeOffset.Now;

            clipRecord.Duration = (int)(endedAt - startedAt).TotalSeconds;
            clipRecord.RecordStoppedAt = endedAt;

            Db.Update(clipRecord);
            await Db.SaveChangesAsync(default);

            return clipRecord;
        }

        private Random _random = new Random();
        private async ValueTask<string?> GetLiveStreamAddress(int roomId, CancellationToken token)
        {
            var realRoomid = await Crawler.GetRealRoomId(roomId, token);
            var allAddresses = await Crawler.GetLiveStreamAddress(realRoomid, token);
            if (allAddresses.Count <= 0) return default;

            return allAddresses[_random.Next(0, allAddresses.Count - 1)].Url;
        }

        /// <summary>
        /// 循环10分钟进行切片，并只保留最近30分钟的切片
        /// </summary>
        /// <param name="roomId"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        private async Task LoopingClipStream(int roomId, CancellationTokenSource cts)
        {
            var token = cts.Token;
            var retry = 0;
            var periodic = TimeSpan.FromMinutes(10);
            while (!token.IsCancellationRequested)
            {
                await RemoveOldClips(periodic, token);

                var address = await GetLiveStreamAddress(roomId, token);
                if (address == default) continue;

                try
                {
                    await PeriodicClip(roomId, address, periodic, cts);
                }
                catch (Exception ex)
                {
                    if (++retry > 10)
                    {
                        Logger.LogError(ex, "Clipping error exceed 10 times, clipper will stop clip");
                        cts.Cancel();
                        throw;
                    }
                    Logger.LogError(ex, "Error when request remote server, clipper will wait 10 seconds");
                    await Task.Delay(TimeSpan.FromSeconds(10), token);
                }
            }
        }

        /// <summary>
        /// 停止循环切片
        /// </summary>
        /// <param name="roomId"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public async ValueTask CancelLoopRecording(int roomId, CancellationToken token)
        {
            using var _cts = _taskController[roomId];
            _taskController.Remove(roomId);
            _cts.Cancel();

            await _tasks[roomId];
            _tasks.Remove(roomId);

            Logger.LogInformation("停止对直播间 #{} 的循环切片", roomId);
        }

        /// <summary>
        /// 开始循环切片
        /// </summary>
        /// <param name="roomId"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task<bool> StartLoopRecording(int roomId, CancellationToken token)
        {
            await _semaphore.WaitAsync(token);
            try
            {
                return await InnerStartLoopRecording(roomId, token);
            }
            finally
            {
                Logger.LogInformation("开始进行直播间 #{} 的循环切片", roomId);
                _semaphore.Release();
            }
        }
        private async Task<bool> InnerStartLoopRecording(int roomId, CancellationToken token)
        {
            if (_taskController.TryGetValue(roomId, out var existToken))
            {
                if (!existToken.IsCancellationRequested)
                    return true;

                await CancelLoopRecording(roomId, token);
            }

            var _clipperCts = new CancellationTokenSource();

            _taskController.Add(roomId, _clipperCts);
            _tasks.Add(roomId, LoopingClipStream(roomId, _clipperCts));
            return true;
        }

        public async Task<LiveStreamRecord> StopDanmakuRecording(int roomId, CancellationToken token)
        {
            if (!_danmakuController.ContainsKey(roomId)) return default!;

            using var _cts = _danmakuController[roomId];
            _danmakuController.Remove(roomId);
            _cts.Cancel();

            var record = await _danmakuTask[roomId];
            _danmakuTask.Remove(roomId);

            return record;
        }

        private async Task<LiveStreamRecord> InnerStartDanmakuRecording(int roomId, string url, CancellationTokenSource cts, int times = 0)
        {
            try
            {
                return await PeriodicClip(roomId, url, TimeSpan.FromMinutes(15), cts, true);
            }
            catch (Exception ex)
            {
                if (times < 4)
                {
                    return await InnerStartDanmakuRecording(roomId, url, cts, times + 1);
                }
                else
                {
                    cts.Cancel();
                    Logger.LogError(ex, "Clipping error, clipper will stop clip");
                    return default!;
                }
            }
        }

        public async Task<bool> StartDanmakuRecording(int roomId, CancellationToken token)
        {
            if (_danmakuController.TryGetValue(roomId, out var existToken))
            {
                if (!existToken.IsCancellationRequested)
                    return true;

                await StopDanmakuRecording(roomId, token);
            }

            var url = await GetLiveStreamAddress(roomId, token);
            if (url == default) return false;


            var _clipperCts = CancellationTokenSource.CreateLinkedTokenSource(token);
            _danmakuController.Add(roomId, _clipperCts);
            _danmakuTask.Add(roomId, InnerStartDanmakuRecording(roomId, url, _clipperCts));

            return true;
        }
    }
}
