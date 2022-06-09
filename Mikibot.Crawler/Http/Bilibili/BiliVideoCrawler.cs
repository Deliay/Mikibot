using Mikibot.Crawler.Http.Bilibili.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mikibot.Crawler.Http.Bilibili
{
    public class BiliVideoCrawler : HttpCrawler
    {
        public async ValueTask<VideoInfo> GetVideoInfo(string? bv, int? av, CancellationToken token = default)
        {
            var url = $"http://api.bilibili.com/x/web-interface/view";
            if (bv != null)
            {
                url = $"{url}?bvid={bv}";
            }
            else if (av.HasValue)
            {
                url = $"{url}?aid={av.Value}";
            }
            else
            {
                throw new ArgumentNullException(nameof(bv), "'av' is empty too");
            }
            var result = await GetAsync<BilibiliApiResponse<VideoInfo>>(url, token);
            result.AssertCode();

            return result.Data;
        }
    }
}
