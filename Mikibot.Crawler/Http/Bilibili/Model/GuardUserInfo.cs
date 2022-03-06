﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Mikibot.Crawler.Http.Bilibili.Model
{
    public struct GuardUserInfo
    {
        public struct Medal
        {
            [JsonPropertyName("medal_name")]
            public string Name { get; set; }

            [JsonPropertyName("medal_level")]
            public int Leve { get; set; }
        }
        [JsonPropertyName("uid")]
        public int Uid { get; set; }

        [JsonPropertyName("ruid")]
        public int RoomUid { get; set; }

        [JsonPropertyName("medal_level")]
        public string UserName { get; set; }

        [JsonPropertyName("rank")]
        public int RoomRank { get; set; }

        [JsonPropertyName("face")]
        public string Avatar { get; set; }

        [JsonPropertyName("is_alive")]
        public bool Online { get; set; }

        [JsonPropertyName("guard_level")]
        public int GuardType { get; set; }

        [JsonPropertyName("medal_info")]
        public Medal MedalInfo { get; set; }
    }
}