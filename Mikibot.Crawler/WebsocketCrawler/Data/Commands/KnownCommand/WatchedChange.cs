using System.Text.Json.Serialization;

namespace Mikibot.Crawler.WebsocketCrawler.Data.Commands.KnownCommand;

public struct WatchedChange
{
    [JsonPropertyName("num")] public uint Count { get; set; }
    [JsonPropertyName("text_small")] public string Text { get; set; }
    [JsonPropertyName("text_large")] public string TextLarge { get; set; }
}
