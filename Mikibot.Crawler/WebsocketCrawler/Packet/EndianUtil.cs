using Mikibot.Crawler.WebsocketCrawler.Packet;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Mikibot.Crawler.WebsocketCrawler.Packet
{
    public static class EndianUtil
    {

        public static BasePacket BytesToStruct(ReadOnlySpan<byte> buffer)
        {
            if (buffer.Length < 16)
            {
                return BasePacket.Empty;
            }

            var result = new BasePacket()
            {
                Size = BinaryPrimitives.ReadUInt32BigEndian(buffer[0..]),
                HeadSize = BinaryPrimitives.ReadUInt16BigEndian(buffer[4..]),
                Version = (ProtocolVersion)BinaryPrimitives.ReadUInt16BigEndian(buffer[6..]),
                Type = (PacketType)BinaryPrimitives.ReadUInt32BigEndian(buffer[8..]),
                Sequence = BinaryPrimitives.ReadUInt32BigEndian(buffer[12..]),
                Data = buffer[16..].ToArray(),
            };
            return result;
        }

        public static byte[] StructToBytes(BasePacket data)
        {
            var size = data.GetSize();
            Span<byte> buffer = stackalloc byte[size];

            BinaryPrimitives.WriteUInt32BigEndian(buffer[0..], data.Size);
            BinaryPrimitives.WriteUInt16BigEndian(buffer[4..], data.HeadSize);
            BinaryPrimitives.WriteUInt16BigEndian(buffer[6..], (ushort)data.Version);
            BinaryPrimitives.WriteUInt32BigEndian(buffer[8..], (uint)data.Type);
            BinaryPrimitives.WriteUInt32BigEndian(buffer[12..], data.Sequence);
            data.Data.CopyTo(buffer[16..]);

            return buffer.ToArray();
        }

    }
}
