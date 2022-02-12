using Mikibot.Crawler.WebsocketCrawler.Packet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mikibot.Crawler.WebsocketCrawler.Data
{
    public static class DataTypeMapping
    {
        private static readonly Dictionary<PacketType, Func<BasePacket, byte[], IData>> TypeMapping = new()
        {
            { PacketType.Online, (p, d) => { Array.Reverse(d); return new OnlineCount() { Type = p.Type, Online = BitConverter.ToUInt32(d) }; } },
            { PacketType.Normal, (p, d) => new Normal() { Type = p.Type, RawContent = Encoding.UTF8.GetString(d) } }
        };

        public static IData Parse(BasePacket packet, byte[] data)
        {
            if (TypeMapping.ContainsKey(packet.Type))
            {
                return TypeMapping[packet.Type](packet, data);
            }
            else
            {
                return new Unknown() { Type = packet.Type, Data = data };
            }
        }
    }
}
