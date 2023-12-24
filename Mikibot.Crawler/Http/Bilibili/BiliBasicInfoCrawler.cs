using Mikibot.Crawler.Http.Bilibili.Model;

namespace Mikibot.Crawler.Http.Bilibili
{
    public class BiliBasicInfoCrawler : HttpCrawler
    {
        public async ValueTask<NavInfo> GetNavInfo(CancellationToken cancellationToken)
        {
            var result = await GetAsync<BilibiliApiResponse<NavInfo>>("https://api.bilibili.com/x/member/web/account", cancellationToken);
            result.AssertCode();

            return result.Data;
        }
    }
}