using System.Text.Json.Serialization;

namespace Mikibot.Crawler.WebsocketCrawler.Data.Commands.KnownCommand;

public struct HotRankSettlementV2
{
    [JsonPropertyName("rank")]
    public int Rank { get; set; }

    [JsonPropertyName("uname")]
    public string UserName { get; set; }

    [JsonPropertyName("face")]
    public string FaceUrl { get; set; }

    [JsonPropertyName("timestamp")]
    public int TimestampSeconds { get; set; }

    [JsonPropertyName("icon")]
    public string IconUrl { get; set; }

    [JsonPropertyName("area_name")]
    public string AreaName { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; }

    [JsonPropertyName("cache_key")]
    public string CacheKey { get; set; }

    [JsonPropertyName("dm_msg")]
    public string Message { get; set; }
}
