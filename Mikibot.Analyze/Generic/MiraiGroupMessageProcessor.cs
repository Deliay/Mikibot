using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Mikibot.Analyze.MiraiHttp;
using Mirai.Net.Data.Messages.Receivers;

namespace Mikibot.Analyze.Generic;

public abstract class MiraiGroupMessageProcessor<T>(IQqService qqService, ILogger<T> logger) where T : MiraiGroupMessageProcessor<T>
{
    protected IQqService QqService => qqService;
    protected ILogger<T> Logger => logger;

    private readonly Channel<GroupMessageReceiver> messageQueue = Channel
    .CreateUnbounded<GroupMessageReceiver>(new UnboundedChannelOptions()
    {
        SingleWriter = true,
        AllowSynchronousContinuations = false,
    });

    private void FilterMessage(GroupMessageReceiver message)
    {
        _ = messageQueue.Writer.WriteAsync(message);
    }

    private async ValueTask Dequeue(CancellationToken token)
    {
        await foreach (var msg in this.messageQueue.Reader.ReadAllAsync(token))
        {
            await Process(msg, token);
        }
    }

    protected abstract ValueTask Process(GroupMessageReceiver message, CancellationToken token = default);

    protected virtual ValueTask PreRun(CancellationToken token) => ValueTask.CompletedTask;

    public async Task Run(CancellationToken token)
    {
        await PreRun(token);
        logger.LogInformation("Mirai group handling message: {}", typeof(T).Name);
        qqService.SubscribeMessage(FilterMessage, token);
        while (!token.IsCancellationRequested)
        {
            try
            {
                await messageQueue.Reader.WaitToReadAsync(token);
                logger.LogInformation("{} Started", typeof(T).Name);
                await Dequeue(token);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "{}", typeof(T).Name);
            }
        }
    }
}