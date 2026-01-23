using Mikibot.Crawler.Http.Bilibili.Model;

namespace Mikibot.Crawler.Http.Bilibili;

public class BiliBasicInfoCrawler(HttpClient client) : HttpCrawler(client)
{
    [Obsolete("Use <see cref=\"GetNavInfoV2\"/> for instead")]
    public async ValueTask<NavInfo> GetNavInfo(CancellationToken cancellationToken = default)
    {
        var result = await GetAsync<BilibiliApiResponse<NavInfo>>("https://api.bilibili.com/x/member/web/account", cancellationToken);
        result.AssertCode();

        return result.Data;
    }

    public async ValueTask<NavInfo> GetNavInfoV2(CancellationToken cancellationToken = default)
    {
        var result = await GetAsync<BilibiliApiResponse<NavInfo>>("https://api.bilibili.com/x/web-interface/nav", cancellationToken);
        result.AssertCode();

        return result.Data;
    } 
}
