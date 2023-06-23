using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Mikibot.Crawler.WebsocketCrawler.Data.Commands.KnownCommand
{
    public struct LikeV3Update
    {
        [JsonPropertyName("click_count")]
        public int ClickCount { get; set; }
    }
}
