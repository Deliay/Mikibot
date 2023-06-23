using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Mikibot.Crawler.WebsocketCrawler.Data.Commands.KnownCommand
{
    public struct EntryEffect
    {
        [JsonPropertyName("uid")]
        public long UserId { get; set; }
        [JsonPropertyName("privilege_type")]
        public int GuardLevel { get; set; }
        [JsonPropertyName("copy_writing")]
        public string CopyWriting { get; set; }
        [JsonPropertyName("trigger_time")]
        [JsonConverter(typeof(UnixNanoSecondOffsetConverter))]
        public DateTimeOffset EnteredAt { get; set; }
    }
}
