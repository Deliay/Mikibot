using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Mikibot.Crawler.WebsocketCrawler.Data.Commands.KnownCommand
{
    public struct RoomRealTimeMessageUpdate
    {
        [JsonPropertyName("roomid")]
        public long RoomId { get; set; }
        [JsonPropertyName("fans")]
        public int Fans { get; set; }
        [JsonPropertyName("fans_club")]
        public int FansClub { get; set; }
    }
}
