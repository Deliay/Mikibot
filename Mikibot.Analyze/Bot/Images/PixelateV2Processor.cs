using Mirai.Net.Data.Messages;
using SixLabors.ImageSharp;

namespace Mikibot.Analyze.Bot.Images;

public class PixelateV2Processor : AbstractPreFrameProcessor
{
    
    private static readonly Func<Frame, Frame> OpenCvProcessor = ImageSharpUtils
        .UseRawDataProcessor(OpenCvUtils.Process);

    public override ValueTask<bool> InitializeAsync(CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(true);
    }

    protected override ValueTask<Frame> ProcessFrame(Frame src, MessageChain messages)
    {
        return ValueTask.FromResult(OpenCvProcessor(src));
    }
}
