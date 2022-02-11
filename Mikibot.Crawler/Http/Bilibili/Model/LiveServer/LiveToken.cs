using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Mikibot.Crawler.Http.Bilibili.Model.LiveServer
{
    public struct LiveToken
    {
        public string Token { get; set; }
        [JsonPropertyName("host_list")]
        public List<LiveServerInfo> Hosts { get; set; }
    }
}
