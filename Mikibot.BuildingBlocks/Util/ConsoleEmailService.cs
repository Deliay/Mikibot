using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mikibot.BuildingBlocks.Util
{
    public class ConsoleEmailService : IEmailService
    {
        public ConsoleEmailService(ILogger<ConsoleEmailService> logger)
        {
            Logger = logger;
        }

        private ILogger<ConsoleEmailService> Logger { get; }

        public ValueTask SendEmail(string to, string subject, string textContent, string htmlContent, string filename, string base64content, CancellationToken token = default)
        {
            Logger.LogInformation("邮件 发给{}, 主题 {}, 内容 {}, 附件 {}", to, subject, textContent is { Length: > 0 } ? textContent : htmlContent, filename);
            return ValueTask.CompletedTask;
        }
    }
}
