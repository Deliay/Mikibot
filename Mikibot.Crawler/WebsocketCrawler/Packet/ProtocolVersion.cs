using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mikibot.Crawler.WebsocketCrawler.Packet
{
    public enum ProtocolVersion : ushort
    {
        Plain = 0,
        Heartbeat = 1,
        ZlibCompressed = 2,
        BrotliCompressed = 3,
    }
}
