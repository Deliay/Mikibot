using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Gif;

namespace Mikibot.Analyze.Bot.Images;

public readonly record struct Frame(
    int Index, Image Image,
    GifFrameMetadata Metadata) : IDisposable
{
    public static Frame Single(Image image) => new(0, image, new GifFrameMetadata()
    {
        FrameDelay = 1,
        HasTransparency = false,
    });
    
    public static Frame Of(int index, Image image) => new(index, image, new GifFrameMetadata()
    {
        FrameDelay = 1,
        HasTransparency = false,
    });

    public void Dispose()
    {
        Image.Dispose();
    }
}