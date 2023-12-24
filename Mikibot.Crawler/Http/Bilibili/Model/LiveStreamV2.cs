using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Mikibot.Crawler.Http.Bilibili.Model
{
    public struct LiveStreamV2
    {
        [JsonPropertyName("protocol_name")]
        public string ProtocolName { get; set; }

        [JsonPropertyName("format")]
        public List<LiveStreamFormatV2> Formats { get; set; }
    }
}
