using System.Runtime.CompilerServices;
using MemeFactory.Core.Processing;
using MemeFactory.Core.Utilities;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Effects;

namespace Mikibot.Analyze.Bot.Images;

public static class Filters
{
    public static Memes.Factory Pixelate()
    {
        return async (image, _, token) =>
        {
            using var sequence = await image.ExtractFrames().ToSequenceAsync(token);

            return await sequence.EachFrame((f, _) =>
            {
                f.Image.Clone(ctx =>
                {
                    var oilSize = Convert.ToInt32(ctx.GetCurrentSize().Width * (1d / 20));
                    var pixelSize = Convert.ToInt32(ctx.GetCurrentSize().Width * (1d / 40));
                    ctx.ApplyProcessor(new OilPaintingProcessor(5, oilSize));
                    ctx.ApplyProcessor(new PixelateProcessor(pixelSize));
                });
                return ValueTask.FromResult(f);
            }, cancellationToken: token).AutoComposeAsync(token);
        };
    }


    public static Memes.Factory Rotation(int circleTimes = 8)
    {
        return (image, _, token) => image.ExtractFrames()
            .Rotation(circleTimes, token)
            .AutoComposeAsync(token);
    }

    public static Memes.Factory Slide()
    {
        return (image, argument, token) =>
        {
            var hor = argument.Contains('右') ? -1 : argument.Contains('左') ? 1 : 0;
            var vert = argument.Contains('下') ? -1 : argument.Contains('上') ? 1 : 0;
            if (hor == 0 && vert == 0) hor = 1;

            return image.ExtractFrames()
                .Slide(hor, vert, cancellationToken: token)
                .AutoComposeAsync(token);
        };
    }
    
    public static Memes.Factory SlideV2()
    {
        return (image, argument, token) =>
        {
            var hor = argument.Contains('右') ? -1 : argument.Contains('左') ? 1 : 0;
            var vert = argument.Contains('下') ? -1 : argument.Contains('上') ? 1 : 0;
            if (hor == 0 && vert == 0) hor = 1;

            return image.ExtractFrames()
                .SlideV2(hor, vert, cancellationToken: token)
                .AutoComposeAsync(token);
        };
    }
    public static async IAsyncEnumerable<Frame> SlideV2(this IAsyncEnumerable<Frame> frames,
        int directionHorizontal = 1, int directionVertical = 0,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        const int minMoves = 10;

        var allFrames = await frames.ToListAsync(cancellationToken);

        // padding to more than `minMoves` frames when not enough
        var targetFrames = allFrames.Count;
        var loopTimes = (minMoves + targetFrames - 1) / targetFrames;
        targetFrames = loopTimes * targetFrames;
        
        var imageSize = allFrames[0].Image.Size;
        var eachX = Convert.ToInt32((1f * imageSize.Width / targetFrames));
        var eachY = Convert.ToInt32((1f * imageSize.Height / targetFrames));

        var finalFrames = allFrames.Loop(loopTimes - 1).ToList();
        for (var i = 0; i < finalFrames.Count; i++)
        {
            using var image = finalFrames[i];
            Image newFrame = new Image<Rgba32>(imageSize.Width, imageSize.Height);
            newFrame.Mutate(ProcessSlide(i, image.Image));
            yield return new Frame() { Sequence = i, Image = newFrame };
        }

        yield break;

        Action<IImageProcessingContext> ProcessSlide(int i, Image image)
        {
            return ctx =>
            {
                var leftX = directionHorizontal != 0 ? 0 - eachX * i : 0;
                var leftY = directionVertical != 0 ? 0 - eachY * i : 0;
                var leftPos = new Point(
                    (leftX) * directionHorizontal,
                    (leftY) * directionVertical);

                var rightX = directionHorizontal != 0 ? imageSize.Width - eachX * i : 0;
                var rightY = directionVertical != 0 ? imageSize.Height - eachY * i : 0;
                var rightPos = new Point(
                    (rightX) * directionHorizontal,
                    (rightY) * directionVertical);

                ctx.DrawImage(image, leftPos, 1f);
                ctx.DrawImage(image, rightPos, 1f);
            };
        }
    }
}
