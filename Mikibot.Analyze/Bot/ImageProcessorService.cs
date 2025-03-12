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
        { "marry", "结婚" },
        { "jerk", "打" },
        { "punch", "👊" },
    };
    protected override ValueTask PreRun(CancellationToken token)
    {
        var autoComposeMemeFolders = Directory.EnumerateDirectories(Path.Combine("resources", "meme", "auto"));
        foreach (var autoComposeMemeFolder in autoComposeMemeFolders)
        {
            _memeProcessors.Add("/" + autoComposeMemeFolder, Memes.AutoCompose(autoComposeMemeFolder));
            if (_knownCommandMapping.TryGetValue(autoComposeMemeFolder, out var knownCommand))
            {
                _memeProcessors.Add("/" + knownCommand, Memes.AutoCompose(autoComposeMemeFolder));
            }
        }
        _memeProcessors.Add("/结婚", Memes.Marry);
        _memeProcessors.Add("/像素化", Filters.Pixelate());
        _memeProcessors.Add("/旋转", Filters.Rotation());
        
        return ValueTask.CompletedTask;
    }

    private Memes.Factory? ComposeAll(string command)
    {
        var possibleCommands = command.Split('/');
        Logger.LogInformation("Split input commands: {}", possibleCommands.GetEnumerator());
        var factories = possibleCommands
            .Select(s => s.Trim())
            .Where(_memeProcessors.ContainsKey)
            .Select(s => _memeProcessors[s])
            .ToList();
        
        Logger.LogInformation("Match {} meme factory", factories.Count);
        
        return factories.Count switch
        {
            1 => factories[0],
            0 => null,
            _ => (async (image, message, token) =>
            {
                var headFactory = factories[0];
                var currentFrame = await headFactory(image, message, token).ConfigureAwait(false);
                foreach (var factory in factories[1..])
                {
                    using var lastFrame = currentFrame;
                    currentFrame = await factory(currentFrame.Image, message, token);
                }

                return currentFrame;
            })
        };
    }
    
    protected override async ValueTask Process(GroupMessageReceiver message, CancellationToken token = default)
    {
        var msg = message.MessageChain.GetPlainMessage();

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
            using var result = await processor(image, message.MessageChain, token);
            return new ImageMessage() { Base64 = await result.ToDataUri(token) };
        });
        
        MessageBase[] result = await Task.WhenAll(processTasks);
        
        await QqService.SendMessageToSomeGroup([message.Sender.Group.Id], token, result);
    }
}