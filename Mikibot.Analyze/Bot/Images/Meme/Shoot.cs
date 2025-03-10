namespace Mikibot.Analyze.Bot.Images.Meme;

public class Shoot : AbstractOverlayZipGiftMemeProcessor
{
    protected override IAsyncEnumerable<Frame> GetMemeSequenceAsync()
    {
        return _frames.ToAsyncEnumerable();
    }

    protected override ValueTask<int> GetMinimalSequenceKeepAsync()
    {
        return ValueTask.FromResult(20);
    }

    private readonly List<Frame> _frames = [];
    
    public override async ValueTask<bool> InitializeAsync(CancellationToken cancellationToken)
    {
        _frames.AddRange((await FrameUtils.LoadFromMemeResourceFolderAsync(nameof(Shoot).ToLower(), cancellationToken)
            .ToListAsync(cancellationToken)).Slow(2));
        
        return _frames.Count > 0;
    }
}