using System.Runtime.CompilerServices;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace Mikibot.Analyze.Bot.Images;

public static class FrameUtils
{
    public static async IAsyncEnumerable<Frame> LoadFromMemeResourceFolderAsync(string name,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var enumerateFiles = Directory
            .EnumerateFiles(Path.Combine("resources", "meme", name), "*.png")
            .Order();

        var fileIndex = 1;
        foreach (var file in enumerateFiles)
        {
            yield return Frame.Of(fileIndex++, await Image.LoadAsync(file, cancellationToken));
        }
    }

    public static async IAsyncEnumerable<Frame> Slow(this IAsyncEnumerable<Frame> src, int times)
    {
        var index = 1;
        await foreach (var frame in src)
        {
            yield return frame with { Index = index++ };
            for (var i = 0; i < times; i++)
            {
                yield return frame with { Index = index++, Image = frame.Image.Clone((_) => {})};
            }
        }
        
    }
}