﻿using System.Reflection;
using MathNet.Numerics;
using MemeFactory.Core.Utilities;
using Microsoft.Extensions.Logging;
using Mikibot.Analyze.Bot.Images;
using Mikibot.Analyze.Generic;
using Mikibot.Analyze.MiraiHttp;
using Mirai.Net.Data.Messages;
using Mirai.Net.Data.Messages.Concretes;
using Mirai.Net.Data.Messages.Receivers;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;

namespace Mikibot.Analyze.Bot;

public class ImageProcessorService(
    IQqService qqService,
    ILogger<ImageProcessorService> logger,
    ILogger<MemeCommandHandler> memeLogger)
    : MiraiGroupMessageProcessor<ImageProcessorService>(qqService, logger)
{
    private readonly MemeCommandHandler memeCommandHandler = new(memeLogger);

    private readonly Dictionary<string, string> _knownCommandMapping = new()
    {
        { "shoot", "射" },
        { "jerk", "打" },
        { "punch", "👊" },
        { "chushou", "触手" },
    };
    protected override ValueTask PreRun(CancellationToken token)
    {
        memeCommandHandler.RegisterStaticMethods(typeof(Memes));
        
        var autoComposeMemeFolders = Directory.EnumerateDirectories(Path.Combine("resources", "meme", "auto"));
        foreach (var autoComposeMemeFolder in autoComposeMemeFolders)
        {
            var triggerWord = Path.GetFileName(autoComposeMemeFolder)!;
            Logger.LogInformation("Add {} meme composer, trigger word: {}", autoComposeMemeFolder, triggerWord);
            memeCommandHandler.Register(triggerWord, "", Memes.AutoCompose(autoComposeMemeFolder));
            if (_knownCommandMapping.TryGetValue(triggerWord, out var knownCommand))
            {
                memeCommandHandler.Register(knownCommand, "", Memes.AutoCompose(autoComposeMemeFolder));
            }
        }
        
        Logger.LogInformation("Total {} meme processor has been registered.", memeCommandHandler.MemeProcessors.Count);
        return ValueTask.CompletedTask;
    }

    protected override async ValueTask Process(GroupMessageReceiver message, CancellationToken token = default)
    {
        var groupId = message.Sender.Group.Id;
        var msg = message.MessageChain.GetPlainMessage().Trim();

        Logger.LogInformation("Meme processor, processing: {}", msg);
        if (!msg.StartsWith('/')) return;

        if (msg.StartsWith("//帮"))
        {
            var helpStr = string.Join(" | ", memeCommandHandler.MemeHelpers
                .Select(p => p.Value is { Length: > 0 }
                    ? $"/{p.Key}:{p.Value}"
                    : $"/{p.Key}"));
            Logger.LogInformation("Sending help text to group {}, content: {}", groupId, helpStr);
            await QqService.SendMessageToSomeGroup([groupId], token, new PlainMessage(helpStr));
            return;
        }
        
        var processor = memeCommandHandler.GetComposePipeline(msg);

        if (processor is null) return;

        var imageMessages = message.MessageChain
            .Concat(message.MessageChain.OfType<QuoteMessage>().SelectMany(q => q.Origin))
            .OfType<ImageMessage>()
            .ToList();
        
        if (imageMessages.Count is 0 or > 10)
        {
            Logger.LogInformation("Too many or no image in message chain! Count: {}",
                imageMessages.Count);
            return;
        }

        var result = await imageMessages.ToAsyncEnumerable()
            .SelectMany(RunImage)
            .ToArrayAsync(token);
        
        await QqService.SendMessageToSomeGroup([groupId], token, result);
        
        return;
        async IAsyncEnumerable<MessageBase> RunImage(ImageMessage imageMessage)
        {
            logger.LogInformation("Processing image, url: {}", imageMessage.Url);
            using var image = await QqService.ReadImageAsync(imageMessage.Url, token);
            using var seq = await image.ExtractFrames().ToSequenceAsync(token);
            var (frames, errors) = processor(seq, token);
            var frameDelay = !msg.Contains("间隔") ? 8 : -1;
            using var imageResult = await frames.AutoComposeAsync(frameDelay, token);
            var frameCount = imageResult.Image.Frames.Count;
            if (frameCount > 1000)
            {
                yield return new PlainMessage($"生成了{frameCount}帧超过1000了，发不出来┑(￣Д ￣)┍");
            }
            else
            {
                yield return new ImageMessage() { Base64 = await imageResult.ToDataUri(token) };
            }
            if (errors is { Count: > 0 })
            {
                yield return new PlainMessage(string.Join('\n', errors.Select(v => v.Message)));
            }
        }
    }
}