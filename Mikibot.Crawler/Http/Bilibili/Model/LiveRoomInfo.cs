using System.Text.Json.Serialization;

namespace Mikibot.Crawler.Http.Bilibili.Model;

public struct LiveRoomInfo
{

    public string Title { get; set; }
    public string Url { get; set; }
    public string Background { get; set; }

    [JsonPropertyName("user_cover")]
    public string UserCover { get; set; }

    [JsonPropertyName("room_id")]
    public long RoomId { get; set; }

    [JsonPropertyName("live_status")]
    public int LiveStatus { get; set; }
}