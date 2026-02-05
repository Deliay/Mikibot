using System.Text.Json.Serialization;

namespace Mikibot.Crawler.WebsocketCrawler.Data.Commands.KnownCommand;

public struct SuperChatUserInfo : IKnownCommand
{
    [JsonPropertyName("guard_level")]
    public int GuardLevel { get; set; }
    [JsonPropertyName("uname")]
    public string UserName { get; set; }
}