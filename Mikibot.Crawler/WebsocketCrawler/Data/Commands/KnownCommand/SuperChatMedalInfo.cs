using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Mikibot.Crawler.WebsocketCrawler.Data.Commands.KnownCommand
{
    public struct SuperChatMedalInfo
    {
        [JsonPropertyName("target_id")]
        public long MedalUserId { get; set; }
        [JsonPropertyName("medal_name")]
        public string Name { get; set; }
        [JsonPropertyName("medal_level")]
        public int Level { get; set; }
        [JsonPropertyName("guard_level")]
        public int GuardLevel { get; set; }

    }
}
