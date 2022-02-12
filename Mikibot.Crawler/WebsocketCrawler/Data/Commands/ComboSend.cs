﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Mikibot.Crawler.WebsocketCrawler.Data.Commands
{
    public struct ComboSend
    {
        public string Action { get; set; }
        [JsonPropertyName("combo_num")]
        public int ComboNum { get; set; }
        [JsonPropertyName("gift_name")]
        public string GiftName { get; set; }
        [JsonPropertyName("uid")]
        public int SenderUid { get; set; }
        [JsonPropertyName("uname")]
        public string SenderName { get; set; }
    }
}
