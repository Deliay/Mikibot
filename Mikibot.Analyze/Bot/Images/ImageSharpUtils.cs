using System.Diagnostics.CodeAnalysis;
using Mikibot.Analyze.MiraiHttp;
using Mikibot.Analyze.Utils;
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
            var (index, img, _) = frame;
            using var frameImage = img;
            using var before = new MemoryStream();
            frameImage.SaveAsPng(before);

            before.Position = 0;
            var after = processor(before.ToArray());
            return Frame.Of(index, Image.Load(after));
        };
    }

    private static void CopyProperties(GifFrameMetadata src, GifFrameMetadata dest)
    {
        dest.DisposalMethod = GifDisposalMethod.RestoreToBackground;
        dest.FrameDelay = src.FrameDelay;
        dest.HasTransparency = src.HasTransparency;
        dest.LocalColorTable = src.LocalColorTable;
        dest.TransparencyIndex = src.TransparencyIndex;
        dest.ColorTableMode = src.ColorTableMode;
    }

    private static void CopyProperties(Frame src, GifFrameMetadata dest)
    {
        CopyProperties(src.Metadata, dest);
    }
    public static void CopyProperties(Frame src, ImageFrame dest)
    {
        CopyProperties(src, dest.Metadata.GetGifMetadata());
    }

    public static Frame Copy(this ImageFrameCollection frameCollection, int i)
    {
        var frame = frameCollection.CloneFrame(i);
        var gifFrameMetadata = (GifFrameMetadata)frameCollection[i].Metadata.GetGifMetadata().DeepClone();
        return new Frame(i, frame, gifFrameMetadata);
    }
    
    public static IEnumerable<Frame> GetFrames(this Image src)
    {
        return Enumerable.Range(0, src.Frames.Count).Select(i => src.Frames.Copy(i));
    }
    
    public static async ValueTask<Image> ReadImageAsync(this IQqService qqService,
        ImageMessage message, CancellationToken cancellationToken = default)
    {
        await using var stream = await qqService.HttpClient.GetStreamAsync(message.Url, cancellationToken);
        return await Image.LoadAsync(stream, cancellationToken);
    }
    
    public static async ValueTask<Image> ProcessMultipleFrameImageAsync(Image image,
        Func<Frame, ValueTask<Frame>> frameProcessor,
        CancellationToken cancellationToken = default)
    {
        var proceedImages = await Enumerable.Range(0, image.Frames.Count)
            .Select(i => image.Frames.Copy(i))
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

        foreach (var (index, proceedImage, _) in proceedImages[1..]) using (proceedImage)
        {
            templateFrame.Frames.InsertFrame(index, proceedImage.Frames.RootFrame);
            CopyProperties(image.Frames[index].Metadata.GetGifMetadata(),
                templateFrame.Frames[index].Metadata.GetGifMetadata());
        }

        return templateFrame;
    }

    public static async ValueTask<string> ToDataUri(this ImageProcessResult result,
        CancellationToken cancellationToken = default)
    {
        await using var afterStream = new MemoryStream();
        await result.Image.SaveAsync(afterStream, result.Encoder, cancellationToken);
        afterStream.Position = 0;
        return DataUri.Build(result.MimeType, Convert.ToBase64String(afterStream.ToArray()));
    }
    
}