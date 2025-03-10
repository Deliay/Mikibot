namespace Mikibot.Analyze.Bot.Images;

public static class EnumerationUtils
{
    public static IEnumerable<T> Loop<T>(this IEnumerable<T> source, int times = 999)
    {
        List<T> cache = [];

        foreach (var item in source)
        {
            cache.Add(item);
            yield return item;
        }

        var currentTimes = 0;
        while (currentTimes++ < times)
        {
            foreach (var item in cache)
            {
                yield return item;
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

    public static IEnumerable<(T a, T b)> LoopZip<T>(this IEnumerable<T> first,
        IEnumerable<T> second, int secondMinimalKeepCount)
    {
        var firstSeq = first.ToList();
        var secondSeq = second.ToList();

        var total = Enumerable.Range(secondMinimalKeepCount, secondSeq.Count)
            .Select(c => Lcm(firstSeq.Count, c))
            .Min();
        
        var shortSeq = firstSeq.Count > secondSeq.Count ? (secondSeq) : (firstSeq);

        var loopTimes = (total / shortSeq.Count) - 1;

        var frames = firstSeq.Count > secondSeq.Count
            ? firstSeq.Zip(secondSeq.Loop(loopTimes))
            : firstSeq.Loop(loopTimes).Zip(secondSeq);
        
        foreach (var tuple in frames)
        {
            yield return tuple;
        }
    }
}
