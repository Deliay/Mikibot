using System.Text.RegularExpressions;
using MathNet.Numerics.Random;
using Microsoft.Extensions.Logging;
using Mikibot.Analyze.Generic;
using Mikibot.Analyze.MiraiHttp;
using Mirai.Net.Data.Messages;
using Mirai.Net.Data.Messages.Concretes;
using Mirai.Net.Data.Messages.Receivers;

namespace Mikibot.Analyze.Bot;

public partial class OptionSelectorService(IQqService qqService, ILogger<OptionSelectorService> logger)
    : MiraiGroupMessageProcessor<OptionSelectorService>(qqService, logger)
{

    private static readonly MessageBase[] NoOptions = [new PlainMessage("没选项，选个屁？")];
    private static readonly MessageBase[] JustOneOption = [new PlainMessage("单选，还要问？")];
    private static readonly MessageBase[] OptionNotEnough = [new PlainMessage("选项不够！老缠？")];

    [GeneratedRegex(@"帮我选((\d+)个)?\s*(.*)$")]
    private static partial Regex GenerateMatchRegex();

    private static readonly Regex MatchRegex = GenerateMatchRegex();

    private Random Random { get; } = new Random();

    protected override async ValueTask Process(GroupMessageReceiver messages, CancellationToken token = default)
    {
        foreach (var msg in messages.MessageChain)
        {
            if (msg is not PlainMessage plain) continue;
            
            var matches = MatchRegex.Matches(plain.Text);
                
            if (matches.Count == 0) return;

            var (selectCount, optionsText) = matches
                .Select(m => (
                    m.Groups[2].Success ? int.Parse(m.Groups[2].Value) : 1,
                    m.Groups[3].Value))
                .FirstOrDefault();
                
            // 选个屁
            if (optionsText is null || optionsText.Length == 0)
            {
                await QqService.SendMessageToGroup(messages.Sender.Group, token, NoOptions);
                return;
            }
            var options = optionsText
                .Split(' ')
                .Select(option => option.Trim())
                .Where(option => option.Length > 0)
                .Select(option => (option, roll: Random.NextDecimal() * 100))
                .ToList();

            var all = options.Select(option => option.roll).Sum();

            var allOptions = options
                .GroupBy(option => option.option)
                .Select(group => (
                    option: group.Key,
                    rank: group.Count(),
                    rolls: group.Select(option => option.roll).ToList()))
                .Select(optionGroup => (
                    optionGroup.option,
                    optionGroup.rank,
                    rollStr: optionGroup.rolls.Count > 1 ? string.Join('+', optionGroup.rolls.Select(i => $"{i / all * 100:0.00}")) + "=" : "",
                    chance: optionGroup.rolls.Sum() / all))
                .OrderByDescending(item => item.chance)
                .Select(item => (item.option, item.rank, item.rollStr, chance: $"{item.chance * 100:0.00}%"))
                .ToList();

            if (allOptions.Count == 1)
            {
                await QqService.SendMessageToGroup(messages.Sender.Group, token, JustOneOption);
                return;
            }
            else if (selectCount >= options.Count)
            {
                await QqService.SendMessageToGroup(messages.Sender.Group, token, OptionNotEnough);
                return;
            }

            var selectedText = string.Join(' ', allOptions.Take(selectCount).Select(item => item.option));
            var optionWithPercentText = string.Join('\n', allOptions.Select(option => $"- {option.option} [{option.rollStr}{option.chance}]"));

            await QqService.SendMessageToGroup(messages.Sender.Group, token,
            [
                new PlainMessage($"从 {options.Count} 个选项中中选 {selectCount} 个\n{optionWithPercentText}\n\n我选: {selectedText}"),
            ]);
            return;
        }
    }
}