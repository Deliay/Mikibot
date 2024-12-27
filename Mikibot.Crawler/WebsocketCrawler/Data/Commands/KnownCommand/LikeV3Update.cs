using System.Text.Json.Serialization;

namespace Mikibot.Crawler.WebsocketCrawler.Data.Commands.KnownCommand;

public struct LikeV3Update
{
    [JsonPropertyName("click_count")]
    public int ClickCount { get; set; }
}