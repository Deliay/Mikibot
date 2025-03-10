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
                var template = frames[0].Image.Frames.CloneFrame(0);
                ImageSharpUtils.CopyProperties(frames[0], template.Frames.RootFrame);
            
                foreach (var frame in frames[1..])
                {
                    template.Frames.InsertFrame(frame.Index, frame.Image.Frames.RootFrame);
                    ImageSharpUtils.CopyProperties(frame, template.Frames[frame.Index]);
                }
            
                return ImageProcessResult.Gif(template);
            }
            case 1:
                return ImageProcessResult.Png(frames[0].Image);
            default:
                throw new InvalidOperationException("Invalid number of frames");
        }
    }
}