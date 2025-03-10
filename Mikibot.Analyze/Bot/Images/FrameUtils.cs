using System.Runtime.CompilerServices;
using SixLabors.ImageSharp;

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
            yield return new Frame(fileIndex++, await Image.LoadAsync(file, cancellationToken));
        }
    }

    public static IEnumerable<Frame> Slow(this IEnumerable<Frame> src, int times)
    {
        var newIndex = 1;
        foreach (var frame in src)
        {
            for (var i = 0; i < times; i++)
            {
                yield return new Frame(newIndex++, frame.Image);
            }
        }
    }
}