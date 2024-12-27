using System.Text.Json.Serialization;

namespace Mikibot.Crawler.Http.Bilibili.Model.LiveServer;

public struct LiveInitInfo
{
    [JsonPropertyName("room_id")]
    public long RoomId { get; set; }

    [JsonPropertyName("uid")]
    public long BId { get; set; }
}