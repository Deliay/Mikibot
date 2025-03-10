using System.Runtime.CompilerServices;
using Mirai.Net.Data.Messages;
using SixLabors.ImageSharp;

namespace Mikibot.Analyze.Bot.Images;

public abstract class AbstractZipGifMemeProcessor : AbstractTimelineProcessor
{
    protected abstract IAsyncEnumerable<Frame> GetMemeSequenceAsync();
    
    protected abstract ValueTask<int> GetMinimalSequenceKeepAsync();
    
    protected abstract ValueTask<Image> MergeAsync(int frameIndex, Image src, Image meme, CancellationToken cancellationToken = default);
    
    protected override async IAsyncEnumerable<Frame> GetFrameSequence(Image src, MessageChain messages,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var srcFrames = src.GetFrames();
        var memeFrames = await GetMemeSequenceAsync().ToListAsync(cancellationToken);
        var minimalSeqCount = await GetMinimalSequenceKeepAsync();

        var frameIndex = 0;
        foreach (var (srcFrame, memeFrame) in srcFrames.LoopZip(memeFrames, minimalSeqCount)) using (srcFrame) using (memeFrame)
        {
            yield return new Frame(
                frameIndex++, 
                await MergeAsync(frameIndex, srcFrame.Image, memeFrame.Image, cancellationToken),
                srcFrame.Metadata);
        }
    }
}