using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Mikibot.Crawler.WebsocketCrawler.Data.Commands.KnownCommand
{
    public struct InteractWord
    {
        [JsonPropertyName("uname")]
        public string UserName { get; set; }
        [JsonPropertyName("uid")]
        public long UserId { get; set; }
        [JsonPropertyName("timestamp")]
        [JsonConverter(typeof(UnixSecondOffsetConverter))]
        public DateTimeOffset InteractAt { get; set; }
        [JsonPropertyName("fans_medal")]
        public InteractFansMedal FansMedal { get; set; }
    }
}
