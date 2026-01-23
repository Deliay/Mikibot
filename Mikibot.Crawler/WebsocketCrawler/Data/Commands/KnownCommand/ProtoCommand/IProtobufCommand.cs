using System.Text.Json.Serialization;

namespace Mikibot.Crawler.WebsocketCrawler.Data.Commands.KnownCommand.ProtoCommand;

public interface IProtobufCommand<T> : IProtobufCommand
{
}

public interface IProtobufCommand
{
    [JsonPropertyName("pb")]
    public string ProtobufData { get; set; }
}
