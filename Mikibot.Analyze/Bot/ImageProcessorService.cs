using System.Reflection;
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

public class ImageProcessorService(IQqService qqService, ILogger<ImageProcessorService> logger)
    : MiraiGroupMessageProcessor<ImageProcessorService>(qqService, logger)
{
    static ImageProcessorService()
    {
        Configuration.Default.ImageFormatsManager.AddImageFormat(PngFormat.Instance);
        Configuration.Default.ImageFormatsManager.AddImageFormat(JpegFormat.Instance);
        Configuration.Default.ImageFormatsManager.AddImageFormat(GifFormat.Instance);
    }

    private readonly Dictionary<string, Memes.Factory> _memeProcessors = [];

    private readonly Dictionary<string, string> _knownCommandMapping = new()
    {
        { "shoot", "射" },
        { "jerk", "打" },
        { "punch", "👊" },
        { "chushou", "触手" },
    };
    protected override ValueTask PreRun(CancellationToken token)
    {
        var autoComposeMemeFolders = Directory.EnumerateDirectories(Path.Combine("resources", "meme", "auto"));
        foreach (var autoComposeMemeFolder in autoComposeMemeFolders)
        {
            var triggerWord = Path.GetFileName(autoComposeMemeFolder)!;
            Logger.LogInformation("Add {} meme composer, trigger word: {}", autoComposeMemeFolder, triggerWord);
            _memeProcessors.Add("/" + triggerWord, Memes.AutoCompose(autoComposeMemeFolder));
            if (_knownCommandMapping.TryGetValue(triggerWord, out var knownCommand))
            {
                _memeProcessors.Add("/" + knownCommand, Memes.AutoCompose(autoComposeMemeFolder));
            }
        }
        _memeProcessors.Add("/结婚", Memes.Marry);
        _memeProcessors.Add("/像素化", Filters.Pixelate());
        _memeProcessors.Add("/旋转", Filters.Rotation());
        _memeProcessors.Add("/滑", Filters.Slide());
        
        return ValueTask.CompletedTask;
    }

    private Memes.NoArgumentFactory? ComposeAll(string command)
    {
        var possibleCommands = command.Split('/', StringSplitOptions.RemoveEmptyEntries);
        Logger.LogInformation("Split input commands: {}", string.Join(", ", possibleCommands));
        var factories = possibleCommands
            .Select(s => s.Trim())
            .Select(s => '/' + s)
            .Select(s => s.Split(':'))
            .Select(s => (s[0], s.Length > 1 ? s[1] : ""))
            .Where(p => _memeProcessors.ContainsKey(p.Item1))
            .Select(p => (factory: _memeProcessors[p.Item1], argument: p.Item2))
            .ToList();
        
        Logger.LogInformation("Match {} meme factory", factories.Count);
        
        return factories.Count switch
        {
            1 => (image, token) => factories[0].factory(image, factories[0].argument, token),
            0 => null,
            _ => (async (image, token) =>
            {
                var head = factories[0];
                var currentFrame = await head.factory(image, head.argument, token).ConfigureAwait(false);
                foreach (var next in factories[1..])
                {
                    using var lastFrame = currentFrame;
                    currentFrame = await next.factory(currentFrame.Image, next.argument, token);
                }

                return currentFrame;
            })
        };
    }
    
    protected override async ValueTask Process(GroupMessageReceiver message, CancellationToken token = default)
    {
        var msg = message.MessageChain.GetPlainMessage().Trim();

        if (!msg.StartsWith('/')) return;
        
        var processor = ComposeAll(msg);

        if (processor is null) return;

        var imageMessages = message.MessageChain
            .Concat(message.MessageChain.OfType<QuoteMessage>().SelectMany(q => q.Origin))
            .OfType<ImageMessage>()
            .ToList();
        
        if (imageMessages.Count is 0 or > 10)
        {
            Logger.LogInformation("User triggered command, but no image message found in message chain! Count: {}",
                imageMessages.Count);
            return;
        }

        var processTasks = imageMessages.Select(async imageMessage =>
        {
            logger.LogInformation("Processing image, url: {}", imageMessage.Url);
            using var image = await QqService.ReadImageAsync(imageMessage.Url, token);
            using var result = await processor(image, token);
            return new ImageMessage() { Base64 = await result.ToDataUri(token) };
        });
        
        MessageBase[] result = await Task.WhenAll(processTasks);
        
        await QqService.SendMessageToSomeGroup([message.Sender.Group.Id], token, result);
    }
}