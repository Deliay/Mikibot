using System.Text.Json.Serialization;

namespace Mikibot.Crawler.WebsocketCrawler.Data.Commands.KnownCommand;

public struct PopularityRedPocketStart
{
    [JsonPropertyName("lot_id")]
    public int LotId { get; set; }

    [JsonPropertyName("sender_uid")]
    public long SenderUserId { get; set; }

    [JsonPropertyName("sender_name")]
    public string SenderName { get; set; }

    [JsonPropertyName("sender_face")]
    public string SenderFaceUrl { get; set; }

    [JsonPropertyName("join_requirement")]
    public int JoinRequirement { get; set; }

    [JsonPropertyName("danmu")]
    public string Danmu { get; set; }

    [JsonPropertyName("current_time")]
    public int CurrentTimestampSeconds { get; set; }

    [JsonPropertyName("start_time")]
    public int StartTimestampSeconds { get; set; }

    [JsonPropertyName("end_time")]
    public int EndTimestampSeconds { get; set; }

    [JsonPropertyName("last_time")]
    public int DurationSeconds { get; set; }

    [JsonPropertyName("remove_time")]
    public int RemoveTimestampSeconds { get; set; }

    [JsonPropertyName("replace_time")]
    public int ReplaceTimestampSeconds { get; set; }

    [JsonPropertyName("total_price")]
    public int TotalPrice { get; set; }
}
