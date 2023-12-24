using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Mikibot.Crawler.Http.Bilibili.Model.LiveServer
{
    public struct LiveInitInfo
    {
        [JsonPropertyName("room_id")]
        public long RoomId { get; set; }

        [JsonPropertyName("uid")]
        public long BId { get; set; }
    }
}
