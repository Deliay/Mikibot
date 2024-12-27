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
        private static readonly byte[] KeepaliveContent = "[Object asswecan]"u8.ToArray();
        private static readonly ArraySegment<byte> KeepaliveData = new BasePacket()
        {
            Type = PacketType.Heartbeat,
            HeadSize = DefaultHeadSize,
            Size = (uint)(KeepaliveContent.Length + DefaultHeadSize),
            Data = KeepaliveContent,
        }.ToByte();
        public static ArraySegment<byte> Keepalive() => KeepaliveData;

        public static readonly BasePacket Empty = new()
        {
            HeadSize = 0,
            Sequence = 0,
            Size = 0,
            Type = PacketType.Empty,
            Version = 0,
        };

        public static BasePacket Auth(long roomId, long uid, string auth)
        {
            var rawAuthPacket = uid > 0
                ? new { uid, roomid = roomId, protover = 3, platform = "web", type = 2, key = auth, }
                : new { uid = 0L, roomid = roomId, protover = 3, platform = "web", type = 2, key = auth, };
            var authPacket = JsonSerializer.Serialize(rawAuthPacket);
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
