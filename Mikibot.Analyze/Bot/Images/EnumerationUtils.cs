using SixLabors.ImageSharp.Processing;

namespace Mikibot.Analyze.Bot.Images;

public static class EnumerationUtils
{
    public static IEnumerable<Frame> Loop(this IEnumerable<Frame> source, int times = 999)
    {
        List<Frame> cache = [];
        int index = 1;
        foreach (var item in source)
        {
            cache.Add(item);
            yield return item with { Index = index++ };
        }

        var currentTimes = 0;
        while (currentTimes++ < times)
        {
            foreach (var item in cache)
            {
                yield return item with { Index = index++, Image = item.Image.Clone((_) => {})};
            }
        }
    }
    private static int Gcd(int a, int b) {
        while (b != 0) {
            var temp = b;
            b = a % b;
            a = temp;
        }
        return a;
    }
    private static int Lcm(int a, int b) {
        var gcd = Gcd(a, b);
        return (a * b) / gcd;
    }

    public static IEnumerable<(Frame a, Frame b)> LoopZip(this IEnumerable<Frame> first,
        IEnumerable<Frame> second, int secondMinimalKeepCount)
    {
        var firstSeq = first.ToList();
        var secondSeq = second.ToList();

        var total = Enumerable.Range(secondMinimalKeepCount, secondSeq.Count)
            .Select(c => Lcm(firstSeq.Count, c))
            .Min();
        
        var shortSeq = firstSeq.Count > secondSeq.Count ? (secondSeq) : (firstSeq);

        var loopTimes = (total / shortSeq.Count) - 1;

        var frames = (firstSeq.Count > secondSeq.Count
            ? firstSeq.Zip(secondSeq.Loop(loopTimes))
            : firstSeq.Loop(loopTimes).Zip(secondSeq)).ToList();
        
        foreach (var tuple in frames)
        {
            yield return tuple;
        }
    }
}
