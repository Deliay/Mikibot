namespace Mikibot.Crawler.WebsocketCrawler.Data.Commands.KnownCommand.ProtoCommand;

public static class ProtobufCommandExtensions
{
    extension<T>(IProtobufCommand<T> command)
    {
        public T Parse()
        {
            var base64ProtoStr = Convert.FromBase64String(command.ProtobufData);
            try
            {
                return ProtoBuf.Serializer.Deserialize<T>(base64ProtoStr);
            }
            catch(Exception e)
            {
                Console.WriteLine("Lagrange.Proto Serialization Error:" + e.Message);
                return  Lagrange.Proto.Serialization.ProtoSerializer.Deserialize<T>(base64ProtoStr);
            }
        }
    }
}
