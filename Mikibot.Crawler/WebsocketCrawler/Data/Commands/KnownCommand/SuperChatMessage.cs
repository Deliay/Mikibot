using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Mikibot.Crawler.WebsocketCrawler.Data.Commands.KnownCommand
{
    public struct SuperChatMessage
    {
        public string Message { get; set; }
        [JsonPropertyName("message_trans")]
        public string TranslatedMessage { get; set; }
        public int Price { get; set; }
        [JsonConverter(typeof(UnixSecondOffsetConverter))]
        public DateTimeOffset SendAt { get; set; }
        [JsonPropertyName("user_info")]
        public SuperChatUserInfo User { get; set; }
        [JsonPropertyName("medal_info")]
        public SuperChatMedalInfo MedalInfo { get; set; }
        [JsonPropertyName("uid")]
        public long UserId { get; set; }

    }
}
