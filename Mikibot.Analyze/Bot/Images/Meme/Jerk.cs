namespace Mikibot.Analyze.Bot.Images.Meme;

public class Jerk : AbstractOverlayZipGiftMemeProcessor
{
    protected override IAsyncEnumerable<Frame> GetMemeSequenceAsync()
    {
        return FrameUtils.LoadFromMemeResourceFolderAsync(nameof(Jerk).ToLower()).Slow(1);
    }

    protected override ValueTask<int> GetMinimalSequenceKeepAsync()
    {
        return ValueTask.FromResult(16);
    }
    
    public override ValueTask<bool> InitializeAsync(CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(true);
    }
}