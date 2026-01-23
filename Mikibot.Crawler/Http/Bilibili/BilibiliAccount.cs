using Mikibot.Crawler.Http.Bilibili.Model;

namespace Mikibot.Crawler.Http.Bilibili;

public class BilibiliAccount(BiliBasicInfoCrawler crawler)
{
    private NavInfo? _navInfo = null;
    private string? _mixinKey;
    public async ValueTask InitializeAsync(CancellationToken cancellationToken = default)
    {
        var info = await crawler.GetNavInfoV2(cancellationToken);
        _navInfo = info;
        _mixinKey = WbiSigner.GetMixinKey(info.WbiInfo);
    }
    
    public long Mid => (_navInfo ?? throw new InvalidOperationException("Please call initialize first")).Mid;

    public string Sign(IEnumerable<KeyValuePair<string, string>> parameters)
    {
        return WbiSigner.SignQueryParameters(_mixinKey, parameters);
    }
}