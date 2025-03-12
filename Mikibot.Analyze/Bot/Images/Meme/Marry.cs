using Mirai.Net.Data.Messages;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace Mikibot.Analyze.Bot.Images.Meme;

public class Marry : AbstractPreFrameProcessor
{
    private Image left = null!;
    private Image right = null!;
    public override async ValueTask<bool> InitializeAsync(CancellationToken cancellationToken = default)
    {
        var frames = await FrameUtils.LoadFromMemeResourceFolderAsync(nameof(Marry).ToLower(), cancellationToken)
            .ToListAsync(cancellationToken);
        
        if (frames.Count < 2) return false;
        
        left = frames[0].Image;
        right = frames[1].Image;

        return true;
    }

    protected override ValueTask<Frame> ProcessFrame(Frame src, MessageChain messages)
    {
        src.Image.Mutate(ctx =>
        {
            using var localLeft = left.Clone((leftCtx) =>
            {
                leftCtx.StableWith(src.Image.Size);
            });
            using var localRight = right.Clone((leftCtx) =>
            {
                leftCtx.StableWith(src.Image.Size);
            });
            var currentSize = ctx.GetCurrentSize();
            ctx
                .DrawImage(localLeft, currentSize.LeftBottom(localLeft.Size), 1.0f)
                .DrawImage(localRight, currentSize.RightBottom(localRight.Size), 1.0f);
        });

        return ValueTask.FromResult(src);
    }
}