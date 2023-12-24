using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Mikibot.Crawler.Http.Bilibili.Model
{
    public struct PersonalInfo
    {
        public struct LiveRoomDetail
        {
            public int LiveStatus { get; set; }
            public string Title { get; set; }
            public string Url { get; set; }
            public string Cover { get; set; }
            [JsonPropertyName("roomid")]
            public long RoomId { get; set; }
        }

        [JsonPropertyName("live_room")]
        public LiveRoomDetail LiveRoom { get; set; }
    }
}
