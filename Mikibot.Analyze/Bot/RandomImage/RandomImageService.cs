using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Mikibot.Analyze.MiraiHttp;
using Mirai.Net.Data.Messages.Concretes;
using Mirai.Net.Data.Messages.Receivers;

namespace Mikibot.Analyze.Bot.RandomImage;

public class RandomImageService(IMiraiService miraiService, ILogger<RandomImageService> logger)
{
    
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
            if (msg.GroupId == "650042418")
            {
                foreach (var raw in msg.MessageChain)
                {
                    if (raw is PlainMessage plain && plain.Text == "来张")
                    {
                        await miraiService.SendMessageToSomeGroup([msg.GroupId], token, 
                        [
                            new ImageMessage() { Url = "https://nas.drakb.me/GetAkumariaEmotion/getEmotion/" }
                        ]);
                    }
                }
            }
        }
    }

    public async Task Run(CancellationToken token)
    {
        logger.LogInformation("随机图图");
        miraiService.SubscribeMessage(FilterMessage, token);
        while (!token.IsCancellationRequested)
        {
            try
            {
                await messageQueue.Reader.WaitToReadAsync(token);
                logger.LogInformation("started B站连接parser...");
                await Dequeue(token);
            }
            catch (Exception ex)
            {
                logger.LogError("B站连接parserr！", ex);
            }
        }
    }
}