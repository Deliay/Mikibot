﻿using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.ColorSpaces;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing.Processors;
using SixLabors.ImageSharp.Processing.Processors.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Mikibot.BuildingBlocks.Util
{
    public static class AiImageColorAdjustUtility
    {
        static AiImageColorAdjustUtility()
        {
            Configuration.Default.ImageFormatsManager.AddImageFormat(PngFormat.Instance);
        }

        public static bool TryAdjust(string prompts, string base64encodedImage, out string adjustedImage)
        {
            if (!prompts.Contains("night,"))
            {
                adjustedImage = base64encodedImage;
                return false;
            }

            var data = Convert.FromBase64String(base64encodedImage);
            using Image image = Image.Load(data);
            
            image.Mutate((ctx) =>
            {
                ctx.ApplyProcessor(new TemperatureProcessor(-15));
                ctx.Brightness(0.95f);
                ctx.Contrast(1.25f);
                ctx.Saturate(1.1f);
            });

            using var ms = new MemoryStream(data.Length);
            image.SaveAsPng(ms);
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