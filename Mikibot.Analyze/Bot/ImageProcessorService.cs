using Microsoft.Extensions.Logging;
using Mikibot.Analyze.Bot.Images;
using Mikibot.Analyze.Generic;
using Mikibot.Analyze.MiraiHttp;
using Mirai.Net.Data.Messages;
using Mirai.Net.Data.Messages.Concretes;
using Mirai.Net.Data.Messages.Receivers;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
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

    private readonly HttpClient httpClient = new();

    private static Func<(int, Image), (int, Image)> WrapStreamProcessor(Func<ReadOnlySpan<byte>, ReadOnlySpan<byte>> processor)
    {
        return frame =>
        {
            var (index, img) = frame;
            using var frameImage = img;
            using var before = new MemoryStream();
            frameImage.SaveAsPng(before);

            before.Position = 0;
            var after = processor(before.ToArray());
            return (index, Image.Load(after));
        };
    }


    private static Image ProcessMultipleFrameImage(Image image, Func<(int, Image), (int, Image)> frameProcessor)
    {
        var proceedImages = Enumerable.Range(0, image.Frames.Count)
            .Select(i => (i, image.Frames.CloneFrame(i)))
            .AsParallel()
            .Select(frameProcessor)
            .OrderBy(f => f.Item1)
            .ToList();
        var templateFrame = proceedImages[0].Item2.Frames.CloneFrame(0);
    
        foreach (var (index, proceedImage) in proceedImages[1..])
        {
            templateFrame.Frames.InsertFrame(index, proceedImage.Frames.RootFrame);
        }

        return templateFrame;
    }

    private static readonly GifEncoder GifEncoder = new();
    private static readonly PngEncoder PngEncoder = new();
    
    private static string BuildDataUri(string mimeType, string base64Data)
    {
        return $"data:{mimeType};base64,{base64Data}";
    }

    private static (IImageEncoder, Image, string) ProcessImage(Image image,
        Func<(int, Image), (int, Image)> frameProcessor)
    {
        if (image.Frames.Count > 1)
        {
            return (GifEncoder, ProcessMultipleFrameImage(image, frameProcessor), "image/gif");
        }
        else
        {
            return (PngEncoder, frameProcessor((0, image)).Item2, "image/png");
        }
    }

    private static async ValueTask<string> ProcessImageFromStreamToDataUri(Stream stream,
        Func<(int, Image), (int, Image)> frameProcessor,
        CancellationToken cancellationToken)
    {
        using var image = await Image.LoadAsync(stream, cancellationToken);
        var (encoder, newImage, mimeType) = ProcessImage(image, frameProcessor);
        using var afterImage = newImage;
        await using var afterStream = new MemoryStream();
        await newImage.SaveAsync(afterStream, encoder, cancellationToken);
        afterStream.Position = 0;
        var base64String = Convert.ToBase64String(afterStream.ToArray());

        return BuildDataUri(mimeType, base64String);
    }
    
    private async Task<ImageMessage> ProcessPixel(ImageMessage message, CancellationToken cancellationToken = default)
    {
        await using var stream = await httpClient.GetStreamAsync(message.Url, cancellationToken);
        var processor = WrapStreamProcessor(PixelProcessor.Process);
        var data = await ProcessImageFromStreamToDataUri(stream, processor, cancellationToken);
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

        IEnumerable<QuoteMessage?> quoteMessages = [message.MessageChain.GetQuoteMessage()];

        var imageMessages = message.MessageChain
            .Concat(quoteMessages.Where(q => q is not null).SelectMany(q => q!.Origin))
            .OfType<ImageMessage>().ToList();
        
        if (imageMessages.Count is 0 or > 10) return;

        var processTasks = imageMessages.Select(imageMessage => processor(imageMessage, token));
        
        MessageBase[] result = await Task.WhenAll(processTasks);
        
        await MiraiService.SendMessageToSomeGroup([message.Sender.Group.Id], token, result);
    }
}