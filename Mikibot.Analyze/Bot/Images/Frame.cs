using SixLabors.ImageSharp;

namespace Mikibot.Analyze.Bot.Images;

public readonly record struct Frame(
    int Index, Image Image,
    int FrameDelay = 0, bool HasTransparency = false)
{
    public static Frame Single(Image image) => new(0, image);
}