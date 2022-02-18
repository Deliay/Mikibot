using Mikibot.Crawler.Http.Bilibili;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mikibot.AutoClipper.Service
{
    public class ClipperService
    {
        public ClipperService(BiliLiveCrawler crawler)
        {
            Crawler = crawler;
        }

        public BiliLiveCrawler Crawler { get; }
        private readonly Dictionary<int, CancellationTokenSource> _taskController = new();
        private readonly Dictionary<int, Task> _tasks = new();
        private readonly SemaphoreSlim _semaphore = new(1);

        private async Task ClipStream(int roomId, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                // 调用API，拿到直播流地址
                var allAddresses = await Crawler.GetLiveStreamAddress(roomId, token);
                if (allAddresses.Count <= 0) continue;

                var address = allAddresses[0].Url;
                
                // 采集10分钟的数据写入

            }
        }

        public async ValueTask CancelRecording(int roomId, CancellationToken token)
        {
            using var _cts = _taskController[roomId];
            _taskController.Remove(roomId);

            await _tasks[roomId];
            _tasks.Remove(roomId);
        }
        private async ValueTask<bool> InnerStartRecording(int roomId, CancellationToken token)
        {
            if (_taskController.ContainsKey(roomId))
            {
                if (!token.IsCancellationRequested)
                    return true;

                await CancelRecording(roomId, token);
            }

            var _clipperCts = CancellationTokenSource.CreateLinkedTokenSource(token);

            _taskController.Add(roomId, _clipperCts);
            _tasks.Add(roomId, ClipStream(roomId, _clipperCts.Token));

            return true;
        }

        public async ValueTask<bool> StartRecording(int roomId, CancellationToken token)
        {
            await _semaphore.WaitAsync(token);
            try
            {
                return await InnerStartRecording(roomId, token);
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
