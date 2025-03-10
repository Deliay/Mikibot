namespace Mikibot.Analyze.Bot.Images.Meme;

public class Shoot : AbstractOverlayZipGiftMemeProcessor
{
    protected override IAsyncEnumerable<Frame> GetMemeSequenceAsync()
    {
        return FrameUtils.LoadFromMemeResourceFolderAsync(nameof(Shoot).ToLower()).Slow(1);
    }

    protected override ValueTask<int> GetMinimalSequenceKeepAsync()
    {
        return ValueTask.FromResult(20);
    }
    
    public override ValueTask<bool> InitializeAsync(CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(true);
    }
}