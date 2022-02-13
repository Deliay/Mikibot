using Mikibot.Crawler.WebsocketCrawler.Package;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Mikibot.Crawler.WebsocketCrawler.Packet
{
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 16)]
    public struct BasePacket
    {
        public uint Size;

        public ushort HeadSize;

        public ProtocolVersion Version = ProtocolVersion.Heartbeat;

        public PacketType Type;

        public uint Sequence = 1;

        public byte[] Data { get; set; }

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

        //public static implicit operator ArraySegment<byte> (BasePacket packet)
        //{
        //    return packet.ToByte();
        //}

        //public static implicit operator BasePacket(byte[] bytes)
        //{
        //    return ToPacket(bytes);
        //}

        public byte[] ToByte()
        {
            return EndianUtil.StructToBytes(this);
        }

        public static BasePacket ToPacket(byte[] bytes)
        {
            return EndianUtil.BytesToStruct(bytes);
        }

    }
}
