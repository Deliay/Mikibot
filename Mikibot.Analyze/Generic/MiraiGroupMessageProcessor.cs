using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Mikibot.Analyze.MiraiHttp;
using Mirai.Net.Data.Messages.Receivers;

namespace Mikibot.Analyze.Generic;

public abstract class MiraiGroupMessageProcessor<T>(IMiraiService miraiService, ILogger<T> logger, string serviceName = nameof(T)) where T : MiraiGroupMessageProcessor<T>
{
    protected IMiraiService MiraiService => miraiService;
    protected ILogger<T> Logger => logger;

    private readonly Channel<GroupMessageReceiver> messageQueue = Channel
    .CreateUnbounded<GroupMessageReceiver>(new UnboundedChannelOptions()
    {
        SingleWriter = true,
        AllowSynchronousContinuations = false,
    });

    protected virtual void FilterMessage(GroupMessageReceiver message)
    {
        _ = messageQueue.Writer.WriteAsync(message);
    }

    protected virtual async ValueTask Dequeue(CancellationToken token)
    {
        await foreach (var msg in this.messageQueue.Reader.ReadAllAsync(token))
        {
            await Process(msg);
        }
    }

    protected abstract ValueTask Process(GroupMessageReceiver message, CancellationToken token = default);

    public async Task Run(CancellationToken token)
    {
        logger.LogInformation("Mirai group handling message: {}", serviceName);
        miraiService.SubscribeMessage(FilterMessage, token);
        while (!token.IsCancellationRequested)
        {
            try
            {
                await messageQueue.Reader.WaitToReadAsync(token);
                logger.LogInformation("{} Started", serviceName);
                await Dequeue(token);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "{}", serviceName);
            }
        }
    }
}