using System.Runtime.CompilerServices;
using MemeFactory.Core.Processing;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Mikibot.Analyze.Bot.Images;

public static class ImageTextWriter
{
    private static readonly FontCollection Fonts = new();
    private static readonly RichTextOptions RichTextOptions;
    private static readonly int FontCanvasWidth;
    private static readonly int FontCanvasLineHeight;
    
    private static IEnumerable<FontFamily> GetSortedFonts()
    {
        return Fonts.Families
            .Where(f => (f.Name.StartsWith("Noto", StringComparison.InvariantCultureIgnoreCase)
                         && f.Name.Contains("CJK", StringComparison.InvariantCultureIgnoreCase))
                        || f.Name == "Segoe UI Emoji")
            .OrderByDescending(f => 0
                + Math.Sign(f.Name.IndexOf("Serif", StringComparison.InvariantCultureIgnoreCase))
                + Math.Sign(f.Name.IndexOf("Sans", StringComparison.InvariantCultureIgnoreCase))
                + 2 * Math.Sign(f.Name.IndexOf("CJK", StringComparison.InvariantCultureIgnoreCase))
                + 4 * Math.Sign(f.Name.IndexOf("SC", StringComparison.InvariantCultureIgnoreCase))
                + Math.Sign(f.Name.IndexOf("Emoji", StringComparison.InvariantCultureIgnoreCase))
                + 4 * Math.Sign(f.Name.IndexOf("CN", StringComparison.InvariantCultureIgnoreCase))
                );
    }
    
    static ImageTextWriter()
    {
        Fonts.AddSystemFonts();

        var fontFamilies = GetSortedFonts().ToList();

        if (fontFamilies.Count == 0) throw new NullReferenceException("Can't load any font!");
        
        var fontFamily = fontFamilies[0];
        IReadOnlyList<FontFamily> fallbackFonts = fontFamilies.Skip(1).ToList();

        Console.WriteLine($"Fonts: {string.Join(',', fontFamilies.Select(f => f.Name))}");
        
        RichTextOptions = new RichTextOptions(new Font(fontFamily, 72f, FontStyle.Regular))
        {
            FallbackFontFamilies = fallbackFonts,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            TextJustification = TextJustification.InterWord,
        };
        
        var totalSize =TextMeasurer.MeasureSize("一一二三四五六七八九十十", RichTextOptions);
        FontCanvasWidth = (int)Math.Round(totalSize.Size.X, MidpointRounding.AwayFromZero);
        FontCanvasLineHeight = (int)Math.Round(totalSize.Size.Y, MidpointRounding.AwayFromZero);
    }

    private static Size GetFontActualSize(int lines, Size size)
    {
        var ratio = size.Width / (float)FontCanvasWidth;
        var newWidth = size.Width;
        var newHeight = (int)(lines * ratio * FontCanvasLineHeight);
        
        return new Size(newWidth, newHeight);
    }

    private static Image<Rgba32> Draw(string text, int lines, Size size)
    {
        var fontImage = new Image<Rgba32>(FontCanvasWidth, FontCanvasLineHeight * lines);
        fontImage.Mutate(textCtx =>
        {
            textCtx.BackgroundColor(Color.White);
            textCtx.DrawText(RichTextOptions, text, Color.Black);
            textCtx.Resize(new ResizeOptions()
            {
                Mode = ResizeMode.Stretch,
                Size = size,
            });
        });

        return fontImage;
    }
    
    public static async IAsyncEnumerable<Frame> WriteText(this IAsyncEnumerable<Frame> sequence, string text,
        [EnumeratorCancellation] CancellationToken token = default)
    {
        var lines = 1 + (text.Length / 10);
        await foreach (var frame in sequence.WithCancellation(token)) using(frame)
        {
            var image = frame.Image;
            var size = GetFontActualSize(lines, image.Size);
            var newImage = new Image<Rgba32>(
                image.Size.Width, 
                image.Size.Height + size.Height);
            
            newImage.Mutate(ctx =>
            {
                ctx.DrawImage(image, 1.0f);
                
                using var fontImage = Draw(text, lines, size);
                ctx.DrawImage(fontImage,new Point(0, image.Height), 1.0f);
            });

            yield return frame with { Image = newImage};
        }
    }
}