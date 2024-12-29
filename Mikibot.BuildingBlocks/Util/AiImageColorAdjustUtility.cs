using SixLabors.Fonts;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing.Processors;
using SixLabors.ImageSharp.Processing.Processors.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SixLabors.ImageSharp.Drawing.Processing;
using System.Numerics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Mikibot.BuildingBlocks.Util
{
    public static class AiImageColorAdjustUtility
    {
        private static readonly Image transparent = Image.Load("resources/transparent.png");
        private static readonly FontCollection fonts = new();
        private static readonly FontFamily fontFamily;
        private static readonly RichTextOptions basic;
        private static readonly RichTextOptions dateFont;
        private static readonly RichTextOptions userFont;
        private static readonly RichTextOptions luckyFont;
        private static readonly RichTextOptions weatherFont;
        private static readonly RichTextOptions watermarkFont;
        private static readonly Color watermarkColor = new (new Argb32(0, 0, 0, 50));
        public static void Initialize() { }

        private static IReadOnlyList<FontFamily> GetEmojiFallback()
        {
            return fonts.TryGet("Segoe UI Emoji", out var emojiFamily) switch
            {
                true => new List<FontFamily>() { emojiFamily },
                _ => new List<FontFamily>()
            };
        }

        private static FontFamily GetEmojiFont()
        {
            return fonts.TryGet("Segoe UI Emoji", out var emojiFamily) switch
            {
                true => emojiFamily,
                _ => default,
            };
        }

        static AiImageColorAdjustUtility()
        {
            Configuration.Default.ImageFormatsManager.AddImageFormat(PngFormat.Instance);
            Configuration.Default.ImageFormatsManager.AddImageFormat(JpegFormat.Instance);
            fonts.AddSystemFonts();
            if (!fonts.TryGet("Noto Serif CJK SC", out fontFamily))
            {
                fontFamily = fonts.GetByCulture(System.Globalization.CultureInfo.CurrentCulture).FirstOrDefault();
                if (fontFamily == default)
                {
                    throw new Exception("Initialize font failed");
                }
            }

            basic = new RichTextOptions(new Font(fontFamily, 36));
            weatherFont = new RichTextOptions(basic) { Origin = new Vector2(48, 1600), Font = new Font(basic.Font, 36), FallbackFontFamilies = GetEmojiFallback() };
            dateFont = new RichTextOptions(basic) { Origin = new Vector2(48, 1770), Font = new Font(basic.Font, 48) };
            userFont = new RichTextOptions(basic) { Origin = new Vector2(48, 1850), Font = new Font(basic.Font, 36), FallbackFontFamilies = GetEmojiFallback() };
            luckyFont = new RichTextOptions(basic) { Origin = new Vector2(810, 1770), Font = new Font(basic.Font, 86) };
            watermarkFont = new RichTextOptions(basic) { Origin = new Vector2(0, 0), Font = new Font(basic.Font, 24) };
        }

        private readonly static IImageProcessor NightTemperatrueProcessor = new TemperatureProcessor(-15);
        private readonly static JpegEncoder CustomJpegEncoder = new()
        {
            Quality = 90,
        };

        public static bool TryAppendLucky(string prompts,
            string date, string lucky, string name,
            string weather,
            string base64encodedImage, out string adjustedImage)
        {
            var data = Convert.FromBase64String(base64encodedImage);
            using Image image = Image.Load(data);

            image.Mutate((ctx) =>
            {
                if (prompts == "")
                {
                    return;
                }
                if (prompts.Contains("night,"))
                {
                    ctx.ApplyProcessor(NightTemperatrueProcessor);
                    ctx.Brightness(0.925f);
                }
                else
                {
                    ctx.Brightness(0.975f);
                }
                ctx.Contrast(1.15f);
                ctx.Saturate(1.05f);
                ctx.DrawImage(transparent, 1);
                ctx.DrawText(weatherFont, weather, Color.Black);
                ctx.DrawText(dateFont, date, Color.Black);
                ctx.DrawText(luckyFont, lucky, Color.Black);
                ctx.DrawText(userFont, name, Color.Black);
                ctx.DrawText(watermarkFont, "该背景图由AI生成", watermarkColor);
            });

            using var ms = new MemoryStream(data.Length);
            image.SaveAsJpeg(ms, CustomJpegEncoder);
            adjustedImage = Convert.ToBase64String(ms.ToArray());
            return true;
        }

        public static bool TryAdjust(string prompts, string base64encodedImage, out string adjustedImage)
        {
            var data = Convert.FromBase64String(base64encodedImage); 
            using Image image = Image.Load(data);
            
            image.Mutate((ctx) =>
            {
                if (prompts == "")
                {
                    return;
                }
                if (prompts.Contains("night,"))
                {
                    ctx.ApplyProcessor(NightTemperatrueProcessor);
                    ctx.Brightness(0.925f);
                }
                else
                {
                    ctx.Brightness(0.975f);
                }
                ctx.Contrast(1.15f);
                ctx.Saturate(1.05f);
                ctx.DrawText(watermarkFont, "该图片由AI生成", watermarkColor);
            });

            using var ms = new MemoryStream(data.Length);
            image.SaveAsJpeg(ms, CustomJpegEncoder);
            adjustedImage = Convert.ToBase64String(ms.ToArray());
            return true;
        }

        public class TemperatureProcessor : FilterProcessor
        {
            private static int Limit(int raw) => raw switch
            {
                > 100 => 100,
                < -100 => -100,
                _ => raw,
            };
            private static ColorMatrix CreateSimpleTemperatureAdjustMatrix(int temperature, int tint = 0)
            {
                temperature = Limit(temperature);
                tint = Limit(tint);

                ColorMatrix m = default;
                m.M11 = 1.0f; m.M51 = 1f / 255 * temperature;
                m.M22 = 1.0f; m.M52 = 1.0f / 255 * tint;
                m.M33 = 1.0f; m.M53 = -1f / 255 * temperature;

                return m;
            }

            public TemperatureProcessor(int temperature, int tint = 0)
                : base(CreateSimpleTemperatureAdjustMatrix(temperature, tint)) { }
        }

    }
}
