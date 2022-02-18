using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Mikibot.Crawler.Http.Bilibili.Model
{
    public struct LiveStreamAddresses
    {
        [JsonPropertyName("durl")]
        public List<LiveStreamAddress> Urls { get; set; }
    }
}
