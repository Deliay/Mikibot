using Microsoft.Extensions.Logging;
using Mikibot.Analyze.Generic;
using Mikibot.Analyze.MiraiHttp;
using Mikibot.Database;
using Mikibot.Database.Model;
using Mirai.Net.Data.Messages.Concretes;
using Mirai.Net.Data.Messages.Receivers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mikibot.Analyze.Bot;

internal class ChatHistoryService(
    IQqService qqService,
    ILogger<ChatHistoryService> logger,
    MikibotDatabaseContext db)
    : MiraiGroupMessageProcessor<ChatHistoryService>(qqService, logger)
{
    protected override async ValueTask Process(GroupMessageReceiver message, CancellationToken token = default)
    {
        var msg = string.Join('\n', message.MessageChain
            .OfType<PlainMessage>()
            .Select((plain) => plain.Text))
            .Trim();

        if (string.IsNullOrWhiteSpace(msg)) return;

        var msgId = message.MessageChain.GetSourceMessage()?.MessageId;
        if (string.IsNullOrWhiteSpace(msgId)) return;
        
        await db.AddAsync(new ChatbotGroupChatHistory
        {
            GroupId = message.GroupId,
            UserId = message.Sender.Id,
            Message = msg,
            MessageId = msgId,
            SentAt = DateTimeOffset.UtcNow,
        }, token);

        await db.SaveChangesAsync(token);
    }
}
