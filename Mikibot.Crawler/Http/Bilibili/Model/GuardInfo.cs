using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Mikibot.Crawler.Http.Bilibili.Model
{
    public struct GuardInfo
    {
        public struct PageInfo
        {
            [JsonPropertyName("num")]
            public int Count { get; set; }

            [JsonPropertyName("now")]
            public int Current { get; set; }

            [JsonPropertyName("page")]
            public int PageCount { get; set; }

        }


        [JsonPropertyName("info")]
        public PageInfo Info { get; set; }

        [JsonPropertyName("list")]
        public List<GuardUserInfo> List { get; set; }

        [JsonPropertyName("top3")]
        public List<GuardUserInfo> Top3 { get; set; }
    }
}
