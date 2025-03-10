using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Png;

namespace Mikibot.Analyze.Bot.Images;

public readonly record struct ImageProcessResult(IImageEncoder Encoder, Image Image, string MimeType) : IDisposable
{
    private static readonly PngEncoder DefaultPngEncoder = new();
    private static readonly GifEncoder DefaultGifEncoder = new();
    
    public static ImageProcessResult Png(Image image) => new(DefaultPngEncoder, image, "image/png");
    public static ImageProcessResult Gif(Image image) => new(DefaultGifEncoder, image, "image/gif");

    public void Dispose()
    {
        Image.Dispose();
    }
}