using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Mikibot.Crawler.WebsocketCrawler.Packet
{
    [StructLayout(LayoutKind.Explicit)]
    public struct BasePacket
    {
        [FieldOffset(offset: 0)]
        public uint Size;

        [FieldOffset(offset: sizeof(uint))]
        public ushort HeadSize;

        [FieldOffset(offset: sizeof(uint) + sizeof(ushort))]
        public ProtocolVersion Version;

        [FieldOffset(offset: sizeof(ProtocolVersion) + sizeof(ushort) + sizeof(ushort))]
        public PacketType Type;

        private const ushort DefaultHeadSize = sizeof(PacketType) + sizeof(ProtocolVersion) + sizeof(ushort) + sizeof(ushort);

        [FieldOffset(offset: DefaultHeadSize)]
        public byte[] Data;

        private static readonly byte[] KeepliveContent = Encoding.UTF8.GetBytes("[Object asswecan]");
        private static readonly ArraySegment<byte> KeepliveData = new BasePacket()
        {
            HeadSize = DefaultHeadSize,
            Size = (uint)(KeepliveContent.Length + DefaultHeadSize),
            Data = KeepliveContent,
        };
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
            var size = GetSize();
            byte[] result = new byte[size];
            IntPtr buffer = Marshal.AllocHGlobal(size);

            try
            {
                Marshal.StructureToPtr(this, buffer, false);
                Marshal.Copy(buffer, result, 0, size);
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }

            return result;
        }

        public static implicit operator ArraySegment<byte> (BasePacket packet)
        {
            return packet.ToByte();
        }

        public static implicit operator BasePacket(byte[] bytes)
        {
            return ToPacket(bytes);
        }

        public static BasePacket ToPacket(byte[] bytes)
        {
            IntPtr buffer = Marshal.AllocHGlobal(bytes.Length);

            try
            {
                Marshal.Copy(bytes, 0, buffer, bytes.Length);
                return Marshal.PtrToStructure<BasePacket>(buffer);
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }
    }
}
