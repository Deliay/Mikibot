using System.Text.Json.Serialization;
using Mikibot.Crawler.WebsocketCrawler.Data.Commands.Utils;

namespace Mikibot.Crawler.WebsocketCrawler.Data.Commands.KnownCommand;

public struct InteractWord : IKnownCommand
{
    [JsonPropertyName("uname")]
    public string UserName { get; set; }
    [JsonPropertyName("uid")]
    public long UserId { get; set; }
    [JsonPropertyName("timestamp")]
    [JsonConverter(typeof(UnixSecondOffsetConverter))]
    public DateTimeOffset InteractAt { get; set; }
    [JsonPropertyName("fans_medal")]
    public InteractFansMedal? FansMedal { get; set; }
}