using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Png;

namespace Mikibot.Analyze.Bot.Images;

public static class ImageProcessorUtils
{
    public static Func<(int, Image), (int, Image)> WrapStreamProcessor(Func<ReadOnlySpan<byte>, ReadOnlySpan<byte>> processor)
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

    private static void CopyProperties(GifFrameMetadata src, GifFrameMetadata dest)
    {
        dest.DisposalMethod = GifDisposalMethod.RestoreToBackground;
        dest.FrameDelay = src.FrameDelay;
        dest.HasTransparency = src.HasTransparency;
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
    
        var rootMetadata = templateFrame.Metadata.GetGifMetadata();
        rootMetadata.RepeatCount = 0;
        
        var rootMetadataFrame = templateFrame.Frames.RootFrame.Metadata.GetGifMetadata();
        CopyProperties(image.Frames.RootFrame.Metadata.GetGifMetadata(), rootMetadataFrame);

        foreach (var (index, proceedImage) in proceedImages[1..])
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

    public static async ValueTask<string> ProcessImageFromStreamToDataUri(this Stream stream,
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
    
}