using Microsoft.Extensions.Logging;
using Mirai.Net.Data.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mikibot.Analyze.MiraiHttp
{
    public class ConsoleMiraiService : IMiraiService
    {
        public ConsoleMiraiService(ILogger<ConsoleMiraiService> logger)
        {
            Logger = logger;
        }

        public ILogger<ConsoleMiraiService> Logger { get; }

        public ValueTask Run()
        {
            Logger.LogInformation("Console mirai service started");
            return ValueTask.CompletedTask;
        }

        public ValueTask SendMessageToAllGroup(CancellationToken token, params MessageBase[] messages)
        {
            Logger.LogInformation("{}", string.Join("", (object[])messages));
            return ValueTask.CompletedTask;
        }
    }
}
