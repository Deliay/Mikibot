using Mirai.Net.Data.Messages;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace Mikibot.Analyze.Bot.Images;

public class PixelateV2Processor : AbstractPreFrameProcessor
{
    
    public override ValueTask<bool> InitializeAsync(CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(true);
    }

    protected override ValueTask<Frame> ProcessFrame(Frame src, MessageChain messages)
    {
        src.Image.Mutate(ctx =>
        {
            ctx.ApplyProcessor(new SixLabors.ImageSharp.Processing.Processors.Effects.PixelateProcessor(10));
        });
        return ValueTask.FromResult(src);
    }
}
