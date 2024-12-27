using Mikibot.Crawler.WebsocketCrawler.Packet;

namespace Mikibot.Crawler.WebsocketCrawler.Data;

public struct Normal : IData
{
    public PacketType Type { get; set; }
    public string RawContent { get; set; }


}