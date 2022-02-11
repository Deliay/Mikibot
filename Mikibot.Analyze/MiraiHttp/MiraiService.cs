using Microsoft.Extensions.Logging;
using Mirai.Net.Data.Messages;
using Mirai.Net.Sessions;
using Mirai.Net.Sessions.Http.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mikibot.Analyze.MiraiHttp
{
    public class MiraiService : IDisposable, IMiraiService
    {
        private MiraiBot Bot { get; }
        public ILogger<MiraiService> Logger { get; }

        public MiraiService(MiraiBotConfig config, ILogger<MiraiService> logger)
        {
            Bot = new MiraiBot()
            {
                Address = config.Address,
                QQ = config.Uid,
                VerifyKey = config.VerifyKey,
            };
            Logger = logger;
        }

        public async ValueTask Run()
        {
            await Bot.LaunchAsync();
        }
        private DateTimeOffset latestSentAt = DateTimeOffset.Now;
        public async ValueTask SendMessageToAllGroup(CancellationToken token, params MessageBase[] messages)
        {
            foreach (var group in Bot.Groups.Value)
            {
#if DEBUG
                if (group.Id != "139528984") continue;
#endif
                if (token.IsCancellationRequested) break;
                if (DateTimeOffset.Now - latestSentAt < TimeSpan.FromSeconds(3))
                {
                    Logger.LogInformation("推送频率限制 3秒后再进行下一次推送");
                    await Task.Delay(3000);
                    latestSentAt = DateTimeOffset.Now;
                }
                Logger.LogInformation("即将推送信息 ({})", group.Id);
                var result = await group.SendGroupMessageAsync(messages);
                Logger.LogInformation("发送消息回执:{}", result);
            }
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Bot.Dispose();
        }
    }
}
