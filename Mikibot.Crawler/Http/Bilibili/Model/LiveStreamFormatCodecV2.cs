using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Mikibot.Crawler.Http.Bilibili.Model
{
    public struct LiveStreamFormatCodecV2
    {
        public struct UrlInfo
        {
            [JsonPropertyName("host")]
            public string Host { get; set; }

            [JsonPropertyName("extra")]
            public string Extra { get; set; }

            [JsonPropertyName("stream_ttl")]
            public int StreamTtl { get; set; }
        }

        [JsonPropertyName("codec_name")]
        public string CodecName { get; set; }

        [JsonPropertyName("current_qn")]
        public int Quality { get; set; }

        [JsonPropertyName("base_url")]
        public string BaesUrl { get; set; }

        [JsonPropertyName("url_info")]
        public List<UrlInfo> UrlInfos { get; set; }
    }
}
