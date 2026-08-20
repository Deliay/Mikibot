using System.Text.Json.Serialization;

namespace Mikibot.Crawler.WebsocketCrawler.Data.Commands.KnownCommand.ProtoCommand;

public struct SendGiftV2 : IProtobufCommand<SendGiftV2Data>
{
    [JsonPropertyName("pb")]
    public string ProtobufData { get; set; }
}