using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Mikibot.Analyze.Generic;
using Mikibot.Analyze.MiraiHttp;
using Mirai.Net.Data.Messages;
using Mirai.Net.Data.Messages.Concretes;
using Mirai.Net.Data.Messages.Receivers;

namespace Mikibot.Analyze.Bot;

public partial class OptionaSelectorService(IMiraiService miraiService, ILogger<OptionaSelectorService> logger)
    : MiraiGroupMessageProcessor<OptionaSelectorService>(miraiService, logger)
{

    private readonly static MessageBase[] NoOptions = [new PlainMessage("没选项，选个屁？")];
    private readonly static MessageBase[] JustOneOption = [new PlainMessage("单选，还要问？")];
    private readonly static MessageBase[] OptionNotEnough = [new PlainMessage("选项不够！老缠？")];

    [GeneratedRegex("帮我选((\\d+)个)?\\s*(.*)$")]
    private static partial Regex GenerateMatchRegex();

    private readonly static Regex MatchRegex = GenerateMatchRegex();

    private Random Random { get; } = new Random();

    protected override async ValueTask Process(GroupMessageReceiver messages, CancellationToken token = default)
    {
        foreach (var msg in messages.MessageChain)
        {
            if (msg is PlainMessage plain)
            {
                var matches = MatchRegex.Matches(plain.Text);
                
                if (matches is null || matches.Count == 0) return;

                var (selectCount, optionsText) = matches.Cast<Match>()
                    .Select(m => (
                        m.Groups[2].Success ? int.Parse(m.Groups[3].Value) : 1,
                        m.Groups[3].Value))
                    .FirstOrDefault();
                
                // 选个屁
                if (optionsText is null || optionsText.Length == 0)
                {
                    await MiraiService.SendMessageToGroup(messages.Sender.Group, token, NoOptions);
                    return;
                }
                
                var options = optionsText.Split(' ');

                // 单选
                if (options.Length == 1)
                {
                    await MiraiService.SendMessageToGroup(messages.Sender.Group, token, JustOneOption);
                    return;
                }

                // 选项不够或者全选
                else if (selectCount >= options.Length)
                {
                    await MiraiService.SendMessageToGroup(messages.Sender.Group, token, OptionNotEnough);
                    return;
                }
                // x选y
                else
                {
                    var allOptions = options
                        .Select(option => (option, chance: Random.Next(1, 101)))
                        .OrderByDescending(item => item.chance)
                        .ToList();

                    var selectedText = string.Join(' ', allOptions.Take(selectCount).Select(item => item.option));
                    var optionWithPercentText = string.Join('\n', allOptions.Select(option => $"{option.option}: {option.chance}%"));

                    await MiraiService.SendMessageToGroup(messages.Sender.Group, token,
                    [
                        new PlainMessage($"从 {options.Length} 个选项中中选 {selectCount} 个\n{optionWithPercentText}\n\n我选: {selectedText}"),
                    ]);
                    return;
                }
            }
        }
    }
}