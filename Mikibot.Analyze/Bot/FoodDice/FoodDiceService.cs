using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mikibot.Analyze.Generic;
using Mikibot.Analyze.MiraiHttp;
using Mikibot.Database;
using Mikibot.Database.Model;
using Mirai.Net.Data.Messages.Concretes;
using Mirai.Net.Data.Messages.Receivers;

namespace Mikibot.Analyze.Bot.FoodDice;

public class FoodDiceService(IQqService qqService, ILogger<FoodDiceService> logger, MikibotDatabaseContext db)
    : MiraiGroupMessageProcessor<FoodDiceService>(qqService, logger)
{
    private readonly IQqService qqService = qqService;
    private static readonly RandomFood Wind = new() { Name = "西北风 (还未配置食物数据库)", Category = "食物" };

    private const string 食物 = "食物";
    private const string 酒 = "酒";
    private const string 饮品 = "饮品";
    private const string 早餐 = "早餐";
    private const string 午餐 = "午餐";
    private const string 晚餐 = "晚餐";
    private const string 宵夜 = "宵夜";
    private const string 下午茶 = "下午茶";

    private async ValueTask<RandomFood[]> RollRandomFood(string category, int count, CancellationToken token)
    {
        return await db.RandomFoods
            .OrderBy(c => EF.Functions.Random())
            .Where(c => c.Category == category)
            .Take(count)
            .ToArrayAsync(token);
    }
    
    protected override async ValueTask Process(GroupMessageReceiver message, CancellationToken token = default)
    {
        var msgObj = message.MessageChain.OfType<PlainMessage>().FirstOrDefault();

        if (msgObj is not { Text.Length: > 0 }) return;
        
        if (!msgObj.Text.StartsWith("今天")) return;
        var msg = msgObj.Text[2..];

        string category;
        int count = 2;
        if (msg.StartsWith("吃什么") || msg.StartsWith("吃啥")) (category, count) = (食物, 3);
        else if (msg.StartsWith("小酌什么")) category = 酒;
        else if (msg.StartsWith("喝什么")) category = 饮品;
        else if (msg.StartsWith("早上吃什么") || msg.StartsWith("早餐吃什么") || msg.StartsWith("早餐吃啥")) (category, count) = (早餐, 2);
        else if (msg.StartsWith("中午吃什么") || msg.StartsWith("午餐吃什么") || msg.StartsWith("午餐吃啥")) (category, count) = (午餐, 3);
        else if (msg.StartsWith("下午吃什么") || msg.StartsWith("下午茶吃什么") || msg.StartsWith("下午茶吃啥")) (category, count) = (下午茶, 2);
        else if (msg.StartsWith("晚上吃什么") || msg.StartsWith("晚餐吃什么") || msg.StartsWith("晚餐吃啥")) (category, count) = (晚餐, 3);
        else if (msg.StartsWith("半夜吃什么") || msg.StartsWith("宵夜吃什么")|| msg.StartsWith("宵夜吃啥")) (category, count) = (宵夜, 2);
        else return;
        
        var foods = await RollRandomFood(category, count, token);
        var name = string.Join("、", foods.Select(f => f.Name));

        await qqService.SendMessageToSomeGroup([message.GroupId], token, new PlainMessage(name));
    }
}