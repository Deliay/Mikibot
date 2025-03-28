﻿using System.Text.Json.Serialization;

namespace Mikibot.Crawler.WebsocketCrawler.Data.Commands.KnownCommand;

public struct InteractFansMedal
{
    [JsonPropertyName("fans_medal")]
    public int GuardLevel { get; set; }
    [JsonPropertyName("medal_level")]
    public int MedalLevel { get; set; }
    [JsonPropertyName("medal_name")]
    public string MedalName { get; set; }
    [JsonPropertyName("target_id")]
    public long FansTagUserId { get; set; }
    [JsonPropertyName("anchor_roomid")]
    public long FansTagRoomId { get; set; }
}