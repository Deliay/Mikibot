using Microsoft.Extensions.Logging;
using Mikibot.Analyze.Bot.Images;
using Mikibot.Analyze.Bot.Images.Meme;
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

    private readonly Dictionary<string, IImageProcessor> _memeProcessors = [];
    
    protected override async ValueTask PreRun(CancellationToken token)
    {
        _memeProcessors.Add("/像素化", new PixelateProcessor());
        _memeProcessors.Add("/像素化v2", new PixelateV2Processor());
        _memeProcessors.Add("/射", new Shoot());
        _memeProcessors.Add("/打", new Jerk());
        
        foreach (var processor in _memeProcessors.Values)
        {
            await processor.InitializeAsync(token);
        }
    }

    protected override async ValueTask Process(GroupMessageReceiver message, CancellationToken token = default)
    {
        var msg = message.MessageChain.GetPlainMessage();
        
        
        if (!_memeProcessors.TryGetValue(msg.Trim(), out var processor)) return;

        var imageMessages = message.MessageChain.OfType<ImageMessage>().ToList();
        
        if (imageMessages.Count is 0 or > 10)
        {
            Logger.LogInformation("User triggered command, but no image message found in message chain! Count: {}",
                imageMessages.Count);
            return;
        }

        var processTasks = imageMessages.Select(async imageMessage =>
        {
            using var image = await QqService.ReadImageAsync(imageMessage, token);
            using var result = await processor.ProcessImage(image, message.MessageChain, token);

            return new ImageMessage() { Base64 = await result.ToDataUri(token) };
        });
        
        MessageBase[] result = await Task.WhenAll(processTasks);
        
        await QqService.SendMessageToSomeGroup([message.Sender.Group.Id], token, result);
    }
}