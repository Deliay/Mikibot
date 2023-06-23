using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Mikibot.Crawler.WebsocketCrawler.Data.Commands.KnownCommand
{
    public struct ComboSend
    {
        [JsonPropertyName("combo_id")]
        public string ComboId { get; set; }
        public string Action { get; set; }
        [JsonPropertyName("combo_num")]
        public int ComboNum { get; set; }
        [JsonPropertyName("gift_name")]
        public string GiftName { get; set; }
        [JsonPropertyName("uid")]
        public long SenderUid { get; set; }
        [JsonPropertyName("uname")]
        public string SenderName { get; set; }
        [JsonPropertyName("combo_total_coin")]
        public int TotalCoin { get; set; }
    }
}
