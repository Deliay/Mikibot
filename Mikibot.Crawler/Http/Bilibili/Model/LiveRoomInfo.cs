using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Mikibot.Crawler.Http.Bilibili.Model
{
    public struct LiveRoomInfo
    {

        public string Title { get; set; }
        public string Url { get; set; }
        public string Background { get; set; }

        [JsonPropertyName("user_cover")]
        public string UserCover { get; set; }

        [JsonPropertyName("room_id")]
        public int RoomId { get; set; }

        [JsonPropertyName("live_status")]
        public int LiveStatus { get; set; }
    }
}
