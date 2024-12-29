using Microsoft.Extensions.Logging;
using Mikibot.Analyze.Generic;
using Mikibot.Analyze.MiraiHttp;
using Mirai.Net.Data.Messages.Concretes;
using Mirai.Net.Data.Messages.Receivers;

namespace Mikibot.Analyze.Bot.RandomImage;

public class RandomImageService(IMiraiService miraiService, ILogger<RandomImageService> logger)
    : MiraiGroupMessageProcessor<RandomImageService>(miraiService, logger, "随机图图")
{
    protected override async ValueTask Process(GroupMessageReceiver message, CancellationToken token = default)
    {

        if (message.GroupId == "650042418")
        {
            foreach (var raw in message.MessageChain)
            {
                if (raw is PlainMessage plain && plain.Text == "来张")
                {
                    await MiraiService.SendMessageToSomeGroup([message.GroupId], token, 
                    [
                        new ImageMessage() { Url = "https://nas.drakb.me/GetAkumariaEmotion/getEmotion/" }
                    ]);
                }
            }
        }
    }
}
