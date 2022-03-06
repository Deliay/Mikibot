using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mikibot.Crawler.WebsocketCrawler.Packet
{
    public enum PacketType : uint
    {
        Empty = int.MaxValue,
        Heartbeat = 2,
        Online = 3,
        Normal = 5,
        Authorize = 7,
        AuthorizeResponse = 8,
    }
}
