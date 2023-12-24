using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mikibot.Crawler.Http.Bilibili
{
    public class BiliLiveStreamCrawler : HttpCrawler
    {
        private const string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/98.0.4758.80 Safari/537.36 Edg/98.0.1108.50";
        public BiliLiveStreamCrawler()
        {
            AddHeader("User-Agent", UserAgent);
            AddHeader("Referer", "https://live.bilibili.com/");
            AddHeader("Origin", "https://live.bilibili.com");
        }

        public async Task<Stream> OpenLiveStream(string url, CancellationToken cancellationToken)
        {
            var res = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            return await res.Content.ReadAsStreamAsync(cancellationToken);
        }

        public async Task OpenLiveStream(string url, Stream @out, CancellationToken cancellationToken)
        {
            var res = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            await res.Content.CopyToAsync(@out, cancellationToken);
        }
    }
}
