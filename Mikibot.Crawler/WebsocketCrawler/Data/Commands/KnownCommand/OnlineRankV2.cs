using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Mikibot.Crawler.WebsocketCrawler.Data.Commands.KnownCommand
{
    public struct OnlineRankV2
    {
        public struct RankUser
        {
            [JsonPropertyName("uname")]
            public string UserName { get; set; }
            [JsonPropertyName("uid")]
            public long UserId { get; set; }
            public int Rank { get; set; }
            public string Score { get; set; }
            [JsonPropertyName("guard_level")]
            public int GuardLevel { get; set; }
        }
        [JsonPropertyName("list")]
        public List<RankUser> RankUsers { get; set; }
    }
}
