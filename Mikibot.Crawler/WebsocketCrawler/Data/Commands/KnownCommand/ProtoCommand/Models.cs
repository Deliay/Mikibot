using Lagrange.Proto;

namespace Mikibot.Crawler.WebsocketCrawler.Data.Commands.KnownCommand.ProtoCommand;

[ProtoPackable]
public partial class FansMedal
{
    [ProtoMember(1)] public long Uid { get; set; }
    [ProtoMember(2)] public uint Level { get; set; }
    [ProtoMember(3)] public string Name { get; set; }
    [ProtoMember(13)] public long LiveRoomId { get; set; }
} 

[ProtoPackable]
public partial class EnterRoomEvent
{
    [ProtoMember(1)] public long Uid { get; set; }
    [ProtoMember(2)] public string Name { get; set; }
    [ProtoMember(6)] public long LiveRoomId { get; set; }
    [ProtoMember(9)] public FansMedal Medal { get; set; }
}
