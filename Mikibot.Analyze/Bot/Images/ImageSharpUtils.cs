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

    public static void CopyProperties(GifFrameMetadata src, GifFrameMetadata dest)
    {
        dest.DisposalMethod = GifDisposalMethod.RestoreToBackground;
        dest.FrameDelay = src.FrameDelay;
        dest.HasTransparency = src.HasTransparency;
    }
    public static void CopyProperties(Frame src, GifFrameMetadata dest)
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
    
    public static Image ProcessMultipleFrameImage(Image image, Func<Frame, Frame> frameProcessor)
    {
        var proceedImages = image.GetFrames()
            .AsParallel()
            .Select(frameProcessor)
            .OrderBy(f => f.Index)
            .ToList();
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

    private static readonly GifEncoder GifEncoder = new();
    private static readonly PngEncoder PngEncoder = new();
    
    private static string BuildDataUri(string mimeType, string base64Data)
    {
        return $"data:{mimeType};base64,{base64Data}";
    }

    private static ImageProcessResult ProcessImage(Image image,
        Func<Frame, Frame> frameProcessor)
    {
        if (image.Frames.Count > 1)
        {
            return ImageProcessResult.Gif(ProcessMultipleFrameImage(image, frameProcessor));
        }
        else
        {
            return ImageProcessResult.Png(frameProcessor(Frame.Single(image)).Image);
        }
    }
    
    public static async ValueTask<string> ProcessImageFromStreamToDataUri(this Stream stream,
        Func<Frame, Frame> frameProcessor,
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

}