using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Mikibot.Crawler.WebsocketCrawler.Packet
{
    public struct BasePacket
    {
        public BasePacket()
        {
        }
        public uint Size = 0;

        public ushort HeadSize = 0;

        public ProtocolVersion Version = ProtocolVersion.Heartbeat;

        public PacketType Type = PacketType.Heartbeat;

        public uint Sequence = 1;

        [NonSerialized]
        public byte[] Data = null!;

        private const int DefaultHeadSize = sizeof(uint) * 3 + sizeof(ushort) * 2;
        private static readonly byte[] KeepliveContent = Encoding.UTF8.GetBytes("[Object asswecan]");
        private static readonly ArraySegment<byte> KeepliveData = new BasePacket()
        {
            Type = PacketType.Heartbeat,
            HeadSize = DefaultHeadSize,
            Size = (uint)(KeepliveContent.Length + DefaultHeadSize),
            Data = KeepliveContent,
        }.ToByte();
        public static ArraySegment<byte> Keeplive() => KeepliveData;

        public readonly static BasePacket Empty = new()
        {
            HeadSize = 0,
            Sequence = 0,
            Size = 0,
            Type = PacketType.Empty,
            Version = 0,
        };

        public static BasePacket Auth(int roomId, string auth)
        {
            var authPacket = JsonSerializer.Serialize(new
            {
                uid = 0,
                roomid = roomId,
                protover = 0,
                platform = "web",
                type = 2,
                key = auth,
            });
            var data = Encoding.UTF8.GetBytes(authPacket);
            return new BasePacket()
            {
                Type = PacketType.Authorize,
                HeadSize = DefaultHeadSize,
                Size = (uint)(DefaultHeadSize + data.Length),
                Data = data,
            };
        }

        public int GetSize()
        {
            return DefaultHeadSize + Data.Length;
        }

        public byte[] ToByte()
        {
            return EndianUtil.StructToBytes(this);
        }

        public static BasePacket ToPacket(ReadOnlyMemory<byte> data)
        {
            return EndianUtil.BytesToStruct(data.Span);
        }

    }
}
