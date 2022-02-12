using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Mikibot.Crawler.WebsocketCrawler.Data.Commands
{
    public struct SendGift
    {
        public string Action { get; set; }
        [JsonPropertyName("coin_type")]
        public string CoinType { get; set; }
        [JsonPropertyName("discount_price")]
        public int DiscountPrice { get; set; }
        public string GiftName { get; set; }
        [JsonConverter(typeof(UnixSecondOffsetConverter))]
        public DateTimeOffset SentAt { get; set; }
        [JsonPropertyName("uid")]
        public int SenderUid { get; set; }
        [JsonPropertyName("uname")]
        public string SenderName { get; set; }
    }
}
