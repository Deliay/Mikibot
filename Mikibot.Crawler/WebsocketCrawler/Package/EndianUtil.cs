using Mikibot.Crawler.WebsocketCrawler.Packet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Mikibot.Crawler.WebsocketCrawler.Package
{
    public static class EndianUtil
    {
        public static void AdjustEndianness(byte[] data)
        {

            Array.Reverse(data, 0, 4);
            Array.Reverse(data, 4, 2);
            Array.Reverse(data, 6, 2);
            Array.Reverse(data, 8, 4);
            Array.Reverse(data, 12, 4);
        }

        public static BasePacket BytesToStruct(byte[] rawData)
        {
            AdjustEndianness(rawData);

            var result = new BasePacket()
            {
                Size = BitConverter.ToUInt32(rawData, 0),
                HeadSize = BitConverter.ToUInt16(rawData, 4),
                Version = (ProtocolVersion)BitConverter.ToUInt16(rawData, 6),
                Type = (PacketType)BitConverter.ToUInt32(rawData, 8),
                Sequence = BitConverter.ToUInt32(rawData, 12),
                Data = rawData[16..],
            };
            AdjustEndianness(rawData);
            return result;
        }

        public static byte[] StructToBytes(BasePacket data)
        {
            var size = data.GetSize();
            byte[] result = new byte[size];
            IntPtr buffer = Marshal.AllocHGlobal(16);

            try
            {
                Marshal.StructureToPtr(data, buffer, false);
                Marshal.Copy(buffer, result, 0, 16);
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }

            data.Data.CopyTo(result, 16);
            AdjustEndianness(result);

            return result;
        }

    }
}
