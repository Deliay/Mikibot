using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Mikibot.Crawler.WebsocketCrawler.Data.Commands
{
    public struct GuardBuy
    {
        public int Uid { get; set; }
        [JsonPropertyName("username")]
        public string UserName { get; set; }
        [JsonPropertyName("guard_level")]
        public string GuardLevel { get; set; }
        public int Price { get; set; }
        [JsonPropertyName("gift_name")]
        public string GiftName { get; set; }
    }
}
