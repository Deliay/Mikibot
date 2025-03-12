using System.Runtime.CompilerServices;
using MemeFactory.Core.Processing;
using MemeFactory.Core.Utilities;
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

    public static IEnumerable<Frame> Loop(this IEnumerable<Frame> source, int times = 999)
    {
        List<Frame> cache = [];
        var index = 1;
        foreach (var item in source)
        {
            cache.Add(item);
            yield return item with { Sequence = index++ };
        }

        var currentTimes = 0;
        while (currentTimes++ < times)
        {
            foreach (var item in cache)
            {
                yield return new Frame(index++, item.Image.Clone((_) => {}));
            }
        }
    }

    public static Memes.Factory Rotation(int circleTimes = 16)
    {
        return (image, _, token) => image.ExtractFrames()
            .Rotation(circleTimes, token)
            .AutoComposeAsync(token);
    }
    
    public static async IAsyncEnumerable<Frame> Rotation(this IAsyncEnumerable<Frame> frames, int circleTimes = 16,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)

    {
        var deg = 360f / circleTimes;
        var allFrames = await frames.ToListAsync(cancellationToken);
        var total = Algorithms.Lcm(allFrames.Count, circleTimes) / allFrames.Count - 1;

        foreach (var frame in allFrames.Loop(total))
        {
            frame.Image.Mutate((ctx) => ctx.Rotate(deg * frame.Sequence));
            yield return frame;
        }
    }
}