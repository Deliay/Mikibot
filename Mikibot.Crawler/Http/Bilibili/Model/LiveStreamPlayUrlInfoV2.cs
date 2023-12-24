using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Mikibot.Crawler.Http.Bilibili.Model
{
    public class LiveStreamPlayUrlInfoV2
    {
        [JsonPropertyName("playurl")]
        public LiveStreamPlayUrlV2 PlayUrl { get; set; }
    }
}
