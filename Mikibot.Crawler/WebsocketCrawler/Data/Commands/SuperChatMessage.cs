using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Mikibot.Crawler.WebsocketCrawler.Data.Commands
{
    public struct SuperChatMessage
    {
        public string Message { get; set; }
        [JsonPropertyName("message_trans")]
        public int TranslatedMessage { get; set; }
        public int Price { get; set; }
        [JsonConverter(typeof(UnixSecondOffsetConverter))]
        public DateTimeOffset SendAt { get; set; }
        [JsonPropertyName("user_info")]
        public SuperChatUserInfo User { get; set; }
        [JsonPropertyName("uid")]
        public int UserId { get; set; }

    }
}
