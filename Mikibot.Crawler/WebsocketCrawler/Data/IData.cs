using Mikibot.Crawler.WebsocketCrawler.Packet;

namespace Mikibot.Crawler.WebsocketCrawler.Data;

public interface IData
{
    public PacketType Type { get; set; }
}