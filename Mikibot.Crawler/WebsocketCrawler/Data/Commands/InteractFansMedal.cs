﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Mikibot.Crawler.WebsocketCrawler.Data.Commands
{
    public struct InteractFansMedal
    {
        [JsonPropertyName("fans_medal")]
        public int GuardLevel { get; set; }
        [JsonPropertyName("medal_level")]
        public int MedalLevel { get; set; }
        [JsonPropertyName("medal_name")]
        public string MedalName { get; set; }
        [JsonPropertyName("target_id")]
        public int FansTagUserId { get; set; }
        [JsonPropertyName("anchor_roomid")]
        public int FansTagRoomId { get; set; }
    }
}
