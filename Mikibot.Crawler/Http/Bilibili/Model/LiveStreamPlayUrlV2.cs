using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Mikibot.Crawler.Http.Bilibili.Model
{
    public struct LiveStreamPlayUrlV2
    {
        [JsonPropertyName("stream")]
        public List<LiveStreamV2> Streams { get; set; }
    }
}
