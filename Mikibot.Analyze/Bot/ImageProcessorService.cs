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

public class ImageProcessorService(IMiraiService miraiService, ILogger<ImageProcessorService> logger)
    : MiraiGroupMessageProcessor<ImageProcessorService>(miraiService, logger)
{
    static ImageProcessorService()
    {
        Configuration.Default.ImageFormatsManager.AddImageFormat(PngFormat.Instance);
        Configuration.Default.ImageFormatsManager.AddImageFormat(JpegFormat.Instance);
        Configuration.Default.ImageFormatsManager.AddImageFormat(GifFormat.Instance);
    }

    private async Task<ImageMessage> ProcessPixel(ImageMessage message, CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Requesting resource form {}", message.Url);
        await using var stream = await MiraiService.HttpClient.GetStreamAsync(message.Url, cancellationToken);
        var processor = ImageProcessorUtils.WrapStreamProcessor(PixelProcessor.Process);
        var data = await stream.ProcessImageFromStreamToDataUri(processor, cancellationToken);
        return new ImageMessage() { Base64 = data };
    }

    private Func<ImageMessage, CancellationToken, Task<ImageMessage>>? GetProcessor(string command)
    {
        if (command.StartsWith("/像素化")) return ProcessPixel;
        else return null;
    }
    
    protected override async ValueTask Process(GroupMessageReceiver message, CancellationToken token = default)
    {
        var msg = message.MessageChain.GetPlainMessage();
        
        var processor = GetProcessor(msg);
        if (processor is null) return;

        var imageMessages = message.MessageChain
            .OfType<ImageMessage>().ToList();
        
        if (imageMessages.Count is 0 or > 10)
        {
            Logger.LogInformation("User triggered command, but no image message found in message chain! Count: {}",
                imageMessages.Count);
            return;
        }

        var processTasks = imageMessages.Select(imageMessage => processor(imageMessage, token));
        
        MessageBase[] result = await Task.WhenAll(processTasks);
        
        await MiraiService.SendMessageToSomeGroup([message.Sender.Group.Id], token, result);
    }
}