namespace Mikibot.Crawler.WebsocketCrawler.Packet;

public enum ProtocolVersion : ushort
{
    Plain = 0,
    Heartbeat = 1,
    ZlibCompressed = 2,
    BrotliCompressed = 3,
}