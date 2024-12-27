using Mikibot.Crawler.WebsocketCrawler.Packet;

namespace Mikibot.Crawler.WebsocketCrawler.Data;

public struct OnlineCount : IData
{
    public PacketType Type { get; set; }
    public uint Online { get; set; }
}