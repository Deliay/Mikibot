using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Mikibot.Crawler.Http.Bilibili.Model
{
    public struct LiveStreamAddressesV2
    {
        [JsonPropertyName("room_id")]
        public long RoomId { get; set; }

        [JsonPropertyName("uid")]
        public long Uid { get; set; }

        [JsonPropertyName("live_status")]
        public int LiveStatus { get; set; }

        [JsonPropertyName("live_time")]
        public long LiveTime { get; set; }

        [JsonPropertyName("playurl_info")]
        public LiveStreamPlayUrlInfoV2 PlayUrlInfo { get; set; }
    }
}
