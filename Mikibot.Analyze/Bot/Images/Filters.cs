﻿using System.Runtime.CompilerServices;
using MemeFactory.Core.Processing;
using MemeFactory.Core.Utilities;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Effects;

namespace Mikibot.Analyze.Bot.Images;

public static class Filters
{
    public static Memes.Factory Pixelate()
    {
        return async (image, _, token) =>
        {
            using var sequence = await image.ExtractFrames().ToSequenceAsync(token);

            return await sequence.EachFrame((f, _) =>
            {
                f.Image.Clone(ctx =>
                {
                    var oilSize = Convert.ToInt32(ctx.GetCurrentSize().Width * (1d / 20));
                    var pixelSize = Convert.ToInt32(ctx.GetCurrentSize().Width * (1d / 40));
                    ctx.ApplyProcessor(new OilPaintingProcessor(5, oilSize));
                    ctx.ApplyProcessor(new PixelateProcessor(pixelSize));
                });
                return ValueTask.FromResult(f);
            }, cancellationToken: token).AutoComposeAsync(token);
        };
    }


    public static Memes.Factory Rotation(int circleTimes = 8)
    {
        return (image, _, token) => image.ExtractFrames()
            .Rotation(circleTimes, token)
            .AutoComposeAsync(token);
    }

    public static Memes.Factory Slide()
    {
        return (image, argument, token) =>
        {
            var hor = argument.Contains('右') ? -1 : argument.Contains('左') ? 1 : 0;
            var vert = argument.Contains('下') ? -1 : argument.Contains('上') ? 1 : 0;
            if (hor == 0 && vert == 0) hor = 1;

            return image.ExtractFrames()
                .Slide(hor, vert, cancellationToken: token)
                .AutoComposeAsync(token);
        };
    }
    
}
