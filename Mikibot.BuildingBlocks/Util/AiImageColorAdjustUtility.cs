﻿using SixLabors.Fonts;
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

namespace Mikibot.BuildingBlocks.Util
{
    public static class AiImageColorAdjustUtility
    {
        private static readonly Image transparent = Image.Load("resources/transparent.png");
        private static readonly FontCollection fonts = new();
        private static readonly FontFamily fontFamily;
        private static readonly TextOptions basic;
        private static readonly TextOptions dateFont;
        private static readonly TextOptions userFont;
        private static readonly TextOptions luckyFont;
        private static readonly TextOptions weatherFont;
        public static void Initialize() { }

        private static IReadOnlyList<FontFamily> GetEmojiFallback()
        {
            return fonts.TryGet("Segoe UI Emoji", out var emojiFamily) switch
            {
                true => new List<FontFamily>() { emojiFamily },
                _ => new List<FontFamily>(),
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

            Console.WriteLine($"Using font: {fontFamily.Name}");
            var emoji = GetEmojiFont();
            Console.WriteLine($"Using emoji: {emoji.Name}");
            var textFallback = new List<FontFamily>() { fontFamily };
            basic = new TextOptions(new Font(fontFamily, 36));
            weatherFont = new TextOptions(basic) { Origin = new Vector2(48, 950), Font = new Font(basic.Font, 36), FallbackFontFamilies = GetEmojiFallback() };
            dateFont = new TextOptions(basic) { Origin = new Vector2(48, 760), Font = new Font(basic.Font, 48) };
            userFont = new TextOptions(basic) { Origin = new Vector2(48, 840), Font = new Font(basic.Font, 36), FallbackFontFamilies = GetEmojiFallback() };
            luckyFont = new TextOptions(basic) { Origin = new Vector2(1650, 930), Font = new Font(basic.Font, 86) };
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
