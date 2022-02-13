using Mikibot.Crawler.WebsocketCrawler.Packet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mikibot.Crawler.WebsocketCrawler.Data
{
    public struct Normal : IData
    {
        public PacketType Type { get; set; }
        public string RawContent { get; set; }


    }
}
