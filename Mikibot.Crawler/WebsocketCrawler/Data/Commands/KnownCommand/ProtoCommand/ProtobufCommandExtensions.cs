using Lagrange.Proto.Serialization;

namespace Mikibot.Crawler.WebsocketCrawler.Data.Commands.KnownCommand.ProtoCommand;

public static class ProtobufCommandExtensions
{
    extension<T>(IProtobufCommand<T> command)
    {
        public T Parse() => ProtoSerializer.Deserialize<T>(Convert.FromBase64String(command.ProtobufData));
    }
}
