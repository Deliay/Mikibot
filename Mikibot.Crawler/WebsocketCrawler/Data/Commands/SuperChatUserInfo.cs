﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Mikibot.Crawler.WebsocketCrawler.Data.Commands
{
    public struct SuperChatUserInfo
    {
        [JsonPropertyName("guard_level")]
        public int GuardLevel { get; set; }
        [JsonPropertyName("user_level")]
        public int FansTagLevel { get; set; }
        [JsonPropertyName("uname")]
        public string UserName { get; set; }
    }
}