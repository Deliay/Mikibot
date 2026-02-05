using System.Text.Json.Serialization;

namespace Mikibot.Crawler.WebsocketCrawler.Data.Commands.KnownCommand;

public struct RoomRealTimeMessageUpdate : IKnownCommand
{
    [JsonPropertyName("roomid")]
    public long RoomId { get; set; }
    [JsonPropertyName("fans")]
    public int Fans { get; set; }
    [JsonPropertyName("fans_club")]
    public int FansClub { get; set; }
}