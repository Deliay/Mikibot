using Mikibot.Analyze.Bot.Images.Meme;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace Mikibot.Analyze.Bot.Images;

public abstract class AbstractOverlayZipGiftMemeProcessor : AbstractZipGifMemeProcessor
{
    protected override ValueTask<Image> MergeAsync(int frameIndex, Image src, Image meme, CancellationToken cancellationToken = default)
    {
        meme.Mutate(ctx => ctx.Resize(src.Size));
        return ValueTask.FromResult(src.Clone(ctx => ctx.DrawImage(meme, 1f)));
    }
}