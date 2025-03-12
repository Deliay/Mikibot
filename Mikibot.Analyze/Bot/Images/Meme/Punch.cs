namespace Mikibot.Analyze.Bot.Images.Meme;

public class Punch : AbstractOverlayZipGiftMemeProcessor
{
    protected override IAsyncEnumerable<Frame> GetMemeSequenceAsync()
    {
        return FrameUtils.LoadFromMemeResourceFolderAsync(nameof(Punch).ToLower());
    }

    protected override ValueTask<int> GetMinimalSequenceKeepAsync()
    {
        return ValueTask.FromResult(26);
    }
    
    public override ValueTask<bool> InitializeAsync(CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(true);
    }
}