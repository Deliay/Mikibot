using System.Runtime.CompilerServices;
using MemeFactory.Core.Processing;
using MemeFactory.Core.Utilities;
using Mirai.Net.Data.Messages;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace Mikibot.Analyze.Bot.Images;

public static class Memes
{
    private static readonly FrameMerger DrawLeftBottom = Composers.Draw(Resizer.Auto, Layout.LeftBottom);
    private static readonly FrameMerger DrawRightCenter = Composers.Draw(Resizer.Auto, Layout.RightCenter);
    
    private static readonly Func<Frame, FrameProcessor> DrawLeftBottomSingle =
        (f) => Composers.Draw(f.Image, Resizer.Auto, Layout.LeftBottom);
    
    private static readonly Func<Frame, FrameProcessor> DrawRightCenterSingle =
        (f) => Composers.Draw(f.Image, Resizer.Auto, Layout.RightCenter);

    public static async ValueTask<MemeResult> SequenceZip(
        Image image, string folder, int slowTimes = 1,
        CancellationToken cancellationToken = default)
    {
        using var sequence = await Frames
            .LoadFromFolderAsync(folder, "*.png", cancellationToken)
            .DuplicateFrame(slowTimes)
            .ToSequenceAsync(cancellationToken);
        using var baseSequence = await image.ExtractFrames().ToSequenceAsync(cancellationToken);;

        var merger = sequence.LcmExpand(-1, cancellationToken);
        
        return await baseSequence
            .FrameBasedZipSequence(merger, DrawLeftBottom, cancellationToken)
            .AutoComposeAsync(3, cancellationToken);
    }

    public delegate ValueTask<MemeResult> NoArgumentFactory(Image image, CancellationToken cancellationToken = default);
    public delegate ValueTask<MemeResult> Factory(Image image, string arguments, CancellationToken cancellationToken = default);
    
    public static Factory AutoCompose(string folder, int slowTimes = 1)
    {
        return (image, msg, token) => SequenceZip(image, folder, slowTimes, token);
    }

    public static async ValueTask<MemeResult> Marry(Image image, string message, CancellationToken cancellationToken)
    {
        var resources = await Frames.LoadFromFolderAsync(Path.Combine("resources", "meme", "marry"), "*.png", cancellationToken)
            .ToListAsync(cancellationToken);
        using var left = resources[0];
        using var right = resources[1];
        
        using var baseSequence = await image.ExtractFrames().ToSequenceAsync(cancellationToken);

        return await baseSequence
            .EachFrame(DrawLeftBottomSingle(left), cancellationToken)
            .EachFrame(DrawRightCenterSingle(right), cancellationToken)
            .AutoComposeAsync(3, cancellationToken);
    }
}
