using Mikibot.Mirai.Crawler.Bilibili.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mikibot.Mirai.Crawler.Bilibili
{
    public class BilibiliCrawler : HttpCrawler
    {
        public const int mxmk = 477317922;
        public const string mxmks = "477317922";

        public async ValueTask<PersonalInfo> GetPersonalInfo(int uid, CancellationToken token = default)
        {
            var result = await GetAsync<BilibiliApiResponse<PersonalInfo>>($"https://api.bilibili.com/x/space/acc/info?mid={uid}", token);
            result.AssertCode();

            return result.Data;
        }

        public async ValueTask<int> GetFollowerCount(int uid, CancellationToken token = default)
        {
            var result = await GetAsync<BilibiliApiResponse<StatInfo>>($"https://api.bilibili.com/x/relation/stat?vmid={uid}", token);
            result.AssertCode();

            return result.Data.Follower;
        }
    }
}
