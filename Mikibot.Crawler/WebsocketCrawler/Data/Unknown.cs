using Mikibot.Crawler.WebsocketCrawler.Packet;

namespace Mikibot.Crawler.WebsocketCrawler.Data;

public struct Unknown : IData
{
    public PacketType Type { get; set; }
    public byte[] Data { get; set; }
}