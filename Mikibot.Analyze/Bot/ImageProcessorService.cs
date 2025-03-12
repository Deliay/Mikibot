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
using SixLabors.ImageSharp.Processing.Processors.Effects;

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
    
    protected override ValueTask PreRun(CancellationToken token)
    {
        var autoComposeMemeFolders = Directory.EnumerateDirectories(Path.Combine("resources", "meme", "auto"));
        foreach (var autoComposeMemeFolder in autoComposeMemeFolders)
        {
            _memeProcessors.Add("/" + autoComposeMemeFolder, Memes.AutoCompose(autoComposeMemeFolder));
        }
        _memeProcessors.Add("/marry", Memes.Marry);
        _memeProcessors.Add("/像素化", Filters.Pixelate());
        
        return ValueTask.CompletedTask;
    }

    protected override async ValueTask Process(GroupMessageReceiver message, CancellationToken token = default)
    {
        var msg = message.MessageChain.GetPlainMessage();
        
        if (!_memeProcessors.TryGetValue(msg.Trim(), out var processor)) return;

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
            var result = await processor(image, message.MessageChain, token);
            return new ImageMessage() { Base64 = await result.ToDataUri(token) };
        });
        
        MessageBase[] result = await Task.WhenAll(processTasks);
        
        await QqService.SendMessageToSomeGroup([message.Sender.Group.Id], token, result);
    }
}