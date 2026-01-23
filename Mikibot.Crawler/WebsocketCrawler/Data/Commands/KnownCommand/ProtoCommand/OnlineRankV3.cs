using System.Text.Json.Serialization;

namespace Mikibot.Crawler.WebsocketCrawler.Data.Commands.KnownCommand.ProtoCommand;

public struct OnlineRankV3 : IProtobufCommand
{
    [JsonPropertyName("pb")]
    public string ProtobufData { get; set; }
}
