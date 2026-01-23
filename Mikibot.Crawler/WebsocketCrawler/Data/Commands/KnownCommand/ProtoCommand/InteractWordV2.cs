using System.Text.Json.Serialization;

namespace Mikibot.Crawler.WebsocketCrawler.Data.Commands.KnownCommand.ProtoCommand;

public struct InteractWordV2 : IProtobufCommand<EnterRoomEvent>
{
    [JsonPropertyName("dmscore")]
    public int DamakuScore { get; set; }

    [JsonPropertyName("pb")]
    public string ProtobufData { get; set; }
}
