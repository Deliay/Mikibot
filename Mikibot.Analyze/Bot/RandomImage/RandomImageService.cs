using Microsoft.Extensions.Logging;
using Mikibot.Analyze.Generic;
using Mikibot.Analyze.MiraiHttp;
using Mirai.Net.Data.Messages.Concretes;
using Mirai.Net.Data.Messages.Receivers;

namespace Mikibot.Analyze.Bot.RandomImage;

public class RandomImageService(IMiraiService miraiService, ILogger<RandomImageService> logger)
    : MiraiGroupMessageProcessor<RandomImageService>(miraiService, logger)
{
    protected override async ValueTask Process(GroupMessageReceiver message, CancellationToken token = default)
    {
        foreach (var raw in message.MessageChain)
        {
            if (raw is PlainMessage plain && plain.Text == "来张毬图")
            {
                await MiraiService.SendMessageToGroup(message.Sender.Group, token, 
                [
                    new ImageMessage() { Url = "https://nas.drakb.me/GetAkumariaEmotion/getEmotion/" }
                ]);
            }
        }
    }
}
