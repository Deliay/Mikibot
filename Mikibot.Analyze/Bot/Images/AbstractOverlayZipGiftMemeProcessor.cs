using Mikibot.Analyze.Bot.Images.Meme;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace Mikibot.Analyze.Bot.Images;

public abstract class AbstractOverlayZipGiftMemeProcessor : AbstractZipGifMemeProcessor
{
    protected override ValueTask<Image> MergeAsync(Image src, Image meme, CancellationToken cancellationToken = default)
    {
        meme.Mutate(ctx => ctx.Resize(src.Size));
        src.Mutate(ctx => ctx.DrawImage(meme, 1));
        
        return ValueTask.FromResult(src);
    }
}