using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Mikibot.Crawler.Http.Bilibili.Model
{
    public struct LiveStreamFormatV2
    {
        [JsonPropertyName("format_name")]
        public string FormatName { get; set; }

        [JsonPropertyName("codec")]
        public List<LiveStreamFormatCodecV2> Codec { get; set; }
    }
}
