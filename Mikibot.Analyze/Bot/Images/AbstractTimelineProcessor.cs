using Mirai.Net.Data.Messages;
using SixLabors.ImageSharp;

namespace Mikibot.Analyze.Bot.Images;

public abstract class AbstractTimelineProcessor : IImageProcessor
{
    public abstract ValueTask<bool> InitializeAsync(CancellationToken cancellationToken);
    protected abstract IAsyncEnumerable<Frame> GetFrameSequence(Image src, MessageChain messages, CancellationToken cancellationToken);

    public async ValueTask<ImageProcessResult> ProcessImage(Image image, MessageChain messages, CancellationToken cancellationToken = default)
    {
        var frames = await GetFrameSequence(image, messages, cancellationToken)
            .OrderBy(f => f.Index)
            .ToListAsync(cancellationToken);

        switch (frames.Count)
        {
            case > 1:
            {
                using var initFrame = frames[0];
                var template = initFrame.Image.Frames.CloneFrame(0);
                template.Metadata.GetGifMetadata().RepeatCount = 0;
                ImageSharpUtils.CopyProperties(initFrame, template.Frames.RootFrame);
            
                foreach (var frame in frames[1..]) using (frame)
                {
                    template.Frames.InsertFrame(frame.Index, frame.Image.Frames.RootFrame);
                    ImageSharpUtils.CopyProperties(frame, template.Frames[frame.Index]);
                }
            
                return ImageProcessResult.Gif(template);
            }
            case 1:
            {
                using var initFrame = frames[0];
                return ImageProcessResult.Png(initFrame.Image);
            }
            default:
                throw new InvalidOperationException("Invalid number of frames");
        }
    }
}