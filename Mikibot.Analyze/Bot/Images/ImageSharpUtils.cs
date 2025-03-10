using Mikibot.Analyze.MiraiHttp;
using Mirai.Net.Data.Messages.Concretes;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Png;

namespace Mikibot.Analyze.Bot.Images;

public static class ImageSharpUtils
{
    public static Func<Frame, Frame> UseRawDataProcessor(Func<ReadOnlySpan<byte>, ReadOnlySpan<byte>> processor)
    {
        return frame =>
        {
            var (index, img, _, _) = frame;
            using var frameImage = img;
            using var before = new MemoryStream();
            frameImage.SaveAsPng(before);

            before.Position = 0;
            var after = processor(before.ToArray());
            return new Frame(index, Image.Load(after));
        };
    }

    private static void CopyProperties(GifFrameMetadata src, GifFrameMetadata dest)
    {
        dest.DisposalMethod = GifDisposalMethod.RestoreToBackground;
        dest.FrameDelay = src.FrameDelay;
        dest.HasTransparency = src.HasTransparency;
    }

    private static void CopyProperties(Frame src, GifFrameMetadata dest)
    {
        dest.DisposalMethod = GifDisposalMethod.RestoreToBackground;
        dest.FrameDelay = src.FrameDelay;
        dest.HasTransparency = src.HasTransparency;
    }
    public static void CopyProperties(Frame src, ImageFrame dest)
    {
        CopyProperties(src, dest.Metadata.GetGifMetadata());
    }

    public static IEnumerable<Frame> GetFrames(this Image src)
    {
        return Enumerable
            .Range(0, src.Frames.Count)
            .Select(i => new Frame(i, src.Frames.CloneFrame(i)));
    }
    
    public static async ValueTask<Image> ReadImageAsync(this IMiraiService miraiService,
        ImageMessage message, CancellationToken cancellationToken = default)
    {
        await using var stream = await miraiService.HttpClient.GetStreamAsync(message.Url, cancellationToken);
        return await Image.LoadAsync(stream, cancellationToken);
    }
    
    public static async ValueTask<Image> ProcessMultipleFrameImageAsync(Image image,
        Func<Frame, ValueTask<Frame>> frameProcessor,
        CancellationToken cancellationToken = default)
    {
        var proceedImages = await Enumerable.Range(0, image.Frames.Count)
            .Select(i => new Frame(i, image.Frames.CloneFrame(i)))
            .AsParallel()
            .Select(frameProcessor)
            .ToAsyncEnumerable()
            .SelectAwait(p => p)
            .OrderBy(f => f.Index)
            .ToListAsync(cancellationToken);
        
        var templateFrame = proceedImages[0].Image.Frames.CloneFrame(0);
    
        var rootMetadata = templateFrame.Metadata.GetGifMetadata();
        rootMetadata.RepeatCount = 0;
        
        var rootMetadataFrame = templateFrame.Frames.RootFrame.Metadata.GetGifMetadata();
        CopyProperties(image.Frames.RootFrame.Metadata.GetGifMetadata(), rootMetadataFrame);

        foreach (var (index, proceedImage, _, _) in proceedImages[1..])
        {
            templateFrame.Frames.InsertFrame(index, proceedImage.Frames.RootFrame);
            CopyProperties(image.Frames[index].Metadata.GetGifMetadata(),
                templateFrame.Frames[index].Metadata.GetGifMetadata());
        }

        return templateFrame;
    }

    private static string BuildDataUri(string mimeType, string base64Data)
    {
        return $"data:{mimeType};base64,{base64Data}";
    }


    public static async ValueTask<string> ToDataUri(this ImageProcessResult result,
        CancellationToken cancellationToken = default)
    {
        await using var afterStream = new MemoryStream();
        await result.Image.SaveAsync(afterStream, result.Encoder, cancellationToken);
        afterStream.Position = 0;
        return BuildDataUri(result.MimeType, Convert.ToBase64String(afterStream.ToArray()));
    }
    
}