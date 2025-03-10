using Mirai.Net.Data.Messages;
using SixLabors.ImageSharp;

namespace Mikibot.Analyze.Bot.Images;

public abstract class AbstractPreFrameProcessor : IImageProcessor
{
    public abstract ValueTask<bool> InitializeAsync(CancellationToken cancellationToken);
    protected abstract ValueTask<Frame> ProcessFrame(Frame src, MessageChain messages);

    public async ValueTask<ImageProcessResult> ProcessImage(Image image,
        MessageChain messages, CancellationToken cancellationToken = default)
    {
        if (image.Frames.Count > 1)
        {
            var result = await ImageSharpUtils
                .ProcessMultipleFrameImageAsync(image,
                    (frame => ProcessFrame(frame, messages)),
                    cancellationToken);
            
            return ImageProcessResult.Gif(result);
        }
        else
        {
            var result = await ProcessFrame(Frame.Single(image), messages);
            
            return ImageProcessResult.Png(result.Image);
        }
    }
}