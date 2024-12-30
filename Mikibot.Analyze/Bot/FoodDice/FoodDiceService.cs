using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mikibot.Analyze.Generic;
using Mikibot.Analyze.MiraiHttp;
using Mikibot.Database;
using Mikibot.Database.Model;
using Mirai.Net.Data.Messages.Concretes;
using Mirai.Net.Data.Messages.Receivers;

namespace Mikibot.Analyze.Bot.FoodDice;

public class FoodDiceService(IMiraiService miraiService, ILogger<FoodDiceService> logger, MikibotDatabaseContext db)
    : MiraiGroupMessageProcessor<FoodDiceService>(miraiService, logger)
{
    private readonly IMiraiService _miraiService = miraiService;
    private static readonly RandomFood Wind = new() { Name = "西北风 (还未配置食物数据库)" };
    
    protected override async ValueTask Process(GroupMessageReceiver message, CancellationToken token = default)
    {
        var msg = message.MessageChain.OfType<PlainMessage>().FirstOrDefault();

        if (msg is not { Text.Length: > 0 }) return;

        if (!msg.Text.StartsWith("今天吃什么")) return;
        
        var food = await db.RandomFoods
            .OrderBy(c => EF.Functions.Random())
            .FirstOrDefaultAsync(token) ?? Wind;

        await _miraiService.SendMessageToSomeGroup([message.GroupId], token,
            new PlainMessage(food.Name));
    }
}