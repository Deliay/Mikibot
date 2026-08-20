using Lagrange.Proto;
using ProtoBuf;
using LgrProtoMember = Lagrange.Proto.ProtoMemberAttribute;
using PbNetProtoMember = ProtoBuf.ProtoMemberAttribute;

namespace Mikibot.Crawler.WebsocketCrawler.Data.Commands.KnownCommand.ProtoCommand;

[ProtoPackable]
[ProtoContract]
public partial class FansMedal
{
    [LgrProtoMember(1)][PbNetProtoMember(1)] public long Uid { get; set; }
    [LgrProtoMember(2)][PbNetProtoMember(2)] public uint Level { get; set; }
    [LgrProtoMember(3)][PbNetProtoMember(3)] public string Name { get; set; }
    [LgrProtoMember(13)][PbNetProtoMember(13)] public long LiveRoomId { get; set; }
} 

[ProtoPackable]
[ProtoContract]
public partial class EnterRoomEvent
{
    [LgrProtoMember(1)][PbNetProtoMember(1)] public long Uid { get; set; }
    [LgrProtoMember(2)][PbNetProtoMember(2)] public string Name { get; set; }
    [LgrProtoMember(6)][PbNetProtoMember(6)] public long LiveRoomId { get; set; }
    [LgrProtoMember(9)][PbNetProtoMember(9)] public FansMedal Medal { get; set; }
}
