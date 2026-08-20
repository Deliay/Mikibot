using Lagrange.Proto;
using ProtoBuf;
using LgrProtoMember = Lagrange.Proto.ProtoMemberAttribute;
using PbNetProtoMember = ProtoBuf.ProtoMemberAttribute;

namespace Mikibot.Crawler.WebsocketCrawler.Data.Commands.KnownCommand.ProtoCommand;

[ProtoPackable]
[ProtoContract]
public partial class SendGiftV2Data
{
    [LgrProtoMember(1)][PbNetProtoMember(1)] public long Uid { get; set; }
    [LgrProtoMember(2)][PbNetProtoMember(2)] public string Uname { get; set; }
    [LgrProtoMember(3)][PbNetProtoMember(3)] public string Face { get; set; }
    [LgrProtoMember(4)][PbNetProtoMember(4)] public string NameColor { get; set; }
    [LgrProtoMember(5)][PbNetProtoMember(5)] public long GuardLevel { get; set; }
    [LgrProtoMember(6)][PbNetProtoMember(6)] public long SvgaBlock { get; set; }
    [LgrProtoMember(7)][PbNetProtoMember(7)] public SendMaster SendMaster { get; set; }
    [LgrProtoMember(8)][PbNetProtoMember(8)] public MedalInfo MedalInfo { get; set; }
    [LgrProtoMember(9)][PbNetProtoMember(9)] public BlindGift BlindGift { get; set; }
    [LgrProtoMember(10)][PbNetProtoMember(10)] public GiftItem GiftList { get; set; }
    [LgrProtoMember(11)][PbNetProtoMember(11)] public bool Switch { get; set; }
    [LgrProtoMember(12)][PbNetProtoMember(12)] public long Test { get; set; }
    [LgrProtoMember(13)][PbNetProtoMember(13)] public WealthInfo WealthInfo { get; set; }
    [LgrProtoMember(14)][PbNetProtoMember(14)] public GroupMedalBrief GroupMedal { get; set; }
    [LgrProtoMember(15)][PbNetProtoMember(15)] public UserInfo SenderUinfo { get; set; }
}

[ProtoPackable]
[ProtoContract]
public partial class GiftItem
{
    [LgrProtoMember(1)][PbNetProtoMember(1)] public long GiftId { get; set; }
    [LgrProtoMember(2)][PbNetProtoMember(2)] public string GiftName { get; set; }
    [LgrProtoMember(3)][PbNetProtoMember(3)] public long Num { get; set; }
    [LgrProtoMember(4)][PbNetProtoMember(4)] public long Demarcation { get; set; }
    [LgrProtoMember(5)][PbNetProtoMember(5)] public long Price { get; set; }
    [LgrProtoMember(6)][PbNetProtoMember(6)] public long DiscountPrice { get; set; }
    [LgrProtoMember(7)][PbNetProtoMember(7)] public long TotalCoin { get; set; }
    [LgrProtoMember(8)][PbNetProtoMember(8)] public string CoinType { get; set; }
    [LgrProtoMember(9)][PbNetProtoMember(9)] public string Tid { get; set; }
    [LgrProtoMember(10)][PbNetProtoMember(10)] public long Timestamp { get; set; }
    [LgrProtoMember(11)][PbNetProtoMember(11)] public long SuperBatchGiftNum { get; set; }
    [LgrProtoMember(12)][PbNetProtoMember(12)] public string BatchComboId { get; set; }
    [LgrProtoMember(13)][PbNetProtoMember(13)] public long ComboResourcesId { get; set; }
    [LgrProtoMember(14)][PbNetProtoMember(14)] public long ComboTotalCoin { get; set; }
    [LgrProtoMember(15)][PbNetProtoMember(15)] public long ComboStayTime { get; set; }
    [LgrProtoMember(16)][PbNetProtoMember(16)] public float Magnification { get; set; }
    [LgrProtoMember(17)][PbNetProtoMember(17)] public bool ShowBatchComboSend { get; set; }
    [LgrProtoMember(18)][PbNetProtoMember(18)] public string Action { get; set; }
    [LgrProtoMember(19)][PbNetProtoMember(19)] public long EffectBlock { get; set; }
    [LgrProtoMember(20)][PbNetProtoMember(20)] public long IsSpecialBatch { get; set; }
    [LgrProtoMember(21)][PbNetProtoMember(21)] public long FloatScResourceId { get; set; }
    [LgrProtoMember(22)][PbNetProtoMember(22)] public string TagImage { get; set; }
    [LgrProtoMember(23)][PbNetProtoMember(23)] public long CritProb { get; set; }
    [LgrProtoMember(24)][PbNetProtoMember(24)] public long Rcost { get; set; }
    [LgrProtoMember(25)][PbNetProtoMember(25)] public long Test { get; set; }
    [LgrProtoMember(26)][PbNetProtoMember(26)] public long FaceEffectType { get; set; }
    [LgrProtoMember(27)][PbNetProtoMember(27)] public long FaceEffectId { get; set; }
    [LgrProtoMember(28)][PbNetProtoMember(28)] public bool IsNaming { get; set; }
    [LgrProtoMember(29)][PbNetProtoMember(29)] public ReceiveUserInfo ReceiveUserInfo { get; set; }
    [LgrProtoMember(30)][PbNetProtoMember(30)] public bool IsJoinReceiver { get; set; }
    [LgrProtoMember(31)][PbNetProtoMember(31)] public BagGiftInfo BagGift { get; set; }
    [LgrProtoMember(32)][PbNetProtoMember(32)] public List<long> GiftTag { get; set; }
    [LgrProtoMember(33)][PbNetProtoMember(33)] public UserInfo ReceiverUinfo { get; set; }
    [LgrProtoMember(34)][PbNetProtoMember(34)] public FaceEffectV2 FaceEffectV2 { get; set; }
    [LgrProtoMember(35)][PbNetProtoMember(35)] public GiftMaterialSnapShot GiftInfo { get; set; }
    [LgrProtoMember(36)][PbNetProtoMember(36)] public long GiftTipPrice { get; set; }
}

[ProtoPackable]
[ProtoContract]
public partial class Base
{
    [LgrProtoMember(1)][PbNetProtoMember(1)] public string Name { get; set; }
    [LgrProtoMember(2)][PbNetProtoMember(2)] public string Face { get; set; }
    [LgrProtoMember(3)][PbNetProtoMember(3)] public int NameColor { get; set; }
    [LgrProtoMember(4)][PbNetProtoMember(4)] public bool IsMystery { get; set; }
    [LgrProtoMember(5)][PbNetProtoMember(5)] public RiskCtrlInfo RiskCtrlInfo { get; set; }
    [LgrProtoMember(6)][PbNetProtoMember(6)] public AccountInfo OriginInfo { get; set; }
    [LgrProtoMember(7)][PbNetProtoMember(7)] public OfficialInfo OfficialInfo { get; set; }
    [LgrProtoMember(8)][PbNetProtoMember(8)] public string NameColorStr { get; set; }
}

[ProtoPackable]
[ProtoContract]
public partial class Medal
{
    [LgrProtoMember(1)][PbNetProtoMember(1)] public string Name { get; set; }
    [LgrProtoMember(2)][PbNetProtoMember(2)] public long Level { get; set; }
    [LgrProtoMember(3)][PbNetProtoMember(3)] public long ColorStart { get; set; }
    [LgrProtoMember(4)][PbNetProtoMember(4)] public long ColorEnd { get; set; }
    [LgrProtoMember(5)][PbNetProtoMember(5)] public long ColorBorder { get; set; }
    [LgrProtoMember(6)][PbNetProtoMember(6)] public long Color { get; set; }
    [LgrProtoMember(7)][PbNetProtoMember(7)] public long Id { get; set; }
    [LgrProtoMember(8)][PbNetProtoMember(8)] public long Type { get; set; }
    [LgrProtoMember(9)][PbNetProtoMember(9)] public long IsLight { get; set; }
    [LgrProtoMember(10)][PbNetProtoMember(10)] public long Ruid { get; set; }
    [LgrProtoMember(11)][PbNetProtoMember(11)] public long GuardLevel { get; set; }
    [LgrProtoMember(12)][PbNetProtoMember(12)] public long Score { get; set; }
    [LgrProtoMember(13)][PbNetProtoMember(13)] public string GuardIcon { get; set; }
    [LgrProtoMember(14)][PbNetProtoMember(14)] public string HonorIcon { get; set; }
    [LgrProtoMember(15)][PbNetProtoMember(15)] public string V2MedalColorStart { get; set; }
    [LgrProtoMember(16)][PbNetProtoMember(16)] public string V2MedalColorEnd { get; set; }
    [LgrProtoMember(17)][PbNetProtoMember(17)] public string V2MedalColorBorder { get; set; }
    [LgrProtoMember(18)][PbNetProtoMember(18)] public string V2MedalColorText { get; set; }
    [LgrProtoMember(19)][PbNetProtoMember(19)] public string V2MedalColorLevel { get; set; }
}

[ProtoPackable]
[ProtoContract]
public partial class UserInfo
{
    [LgrProtoMember(1)][PbNetProtoMember(1)] public long Uid { get; set; }
    [LgrProtoMember(2)][PbNetProtoMember(2)] public Base Base { get; set; }
    [LgrProtoMember(3)][PbNetProtoMember(3)] public Medal Medal { get; set; }
}

[ProtoPackable]
[ProtoContract]
public partial class ReceiveUserInfo
{
    [LgrProtoMember(1)][PbNetProtoMember(1)] public string Uname { get; set; }
    [LgrProtoMember(2)][PbNetProtoMember(2)] public long Uid { get; set; }
}

[ProtoPackable]
[ProtoContract]
public partial class SendMaster
{
    [LgrProtoMember(1)][PbNetProtoMember(1)] public long Uid { get; set; }
    [LgrProtoMember(2)][PbNetProtoMember(2)] public string Uname { get; set; }
}

[ProtoPackable]
[ProtoContract]
public partial class BlindGift
{
    [LgrProtoMember(1)][PbNetProtoMember(1)] public long BlindGiftConfigId { get; set; }
    [LgrProtoMember(2)][PbNetProtoMember(2)] public long OriginalGiftId { get; set; }
    [LgrProtoMember(3)][PbNetProtoMember(3)] public string OriginalGiftName { get; set; }
    [LgrProtoMember(4)][PbNetProtoMember(4)] public long From { get; set; }
    [LgrProtoMember(5)][PbNetProtoMember(5)] public string GiftAction { get; set; }
    [LgrProtoMember(6)][PbNetProtoMember(6)] public long OriginalGiftPrice { get; set; }
    [LgrProtoMember(7)][PbNetProtoMember(7)] public long GiftTipPrice { get; set; }
}

[ProtoPackable]
[ProtoContract]
public partial class MedalInfo
{
    [LgrProtoMember(1)][PbNetProtoMember(1)] public long TargetId { get; set; }
    [LgrProtoMember(2)][PbNetProtoMember(2)] public string Special { get; set; }
    [LgrProtoMember(3)][PbNetProtoMember(3)] public string AnchorUname { get; set; }
    [LgrProtoMember(4)][PbNetProtoMember(4)] public long AnchorRoomid { get; set; }
    [LgrProtoMember(5)][PbNetProtoMember(5)] public long MedalLevel { get; set; }
    [LgrProtoMember(6)][PbNetProtoMember(6)] public string MedalName { get; set; }
    [LgrProtoMember(7)][PbNetProtoMember(7)] public long MedalColor { get; set; }
    [LgrProtoMember(8)][PbNetProtoMember(8)] public long MedalColorStart { get; set; }
    [LgrProtoMember(9)][PbNetProtoMember(9)] public long MedalColorEnd { get; set; }
    [LgrProtoMember(10)][PbNetProtoMember(10)] public long MedalColorBorder { get; set; }
    [LgrProtoMember(11)][PbNetProtoMember(11)] public long IsLighted { get; set; }
    [LgrProtoMember(12)][PbNetProtoMember(12)] public long GuardLevel { get; set; }
}

[ProtoPackable]
[ProtoContract]
public partial class BagGiftInfo
{
    [LgrProtoMember(1)][PbNetProtoMember(1)] public int ShowPrice { get; set; }
    [LgrProtoMember(2)][PbNetProtoMember(2)] public long PriceForShow { get; set; }
}

[ProtoPackable]
[ProtoContract]
public partial class WealthInfo
{
    [LgrProtoMember(1)][PbNetProtoMember(1)] public long Level { get; set; }
}

[ProtoPackable]
[ProtoContract]
public partial class FaceEffectV2
{
    [LgrProtoMember(1)][PbNetProtoMember(1)] public long Id { get; set; }
    [LgrProtoMember(2)][PbNetProtoMember(2)] public long Type { get; set; }
}

[ProtoPackable]
[ProtoContract]
public partial class GroupMedalBrief
{
    [LgrProtoMember(1)][PbNetProtoMember(1)] public long MedalId { get; set; }
    [LgrProtoMember(2)][PbNetProtoMember(2)] public string Name { get; set; }
    [LgrProtoMember(3)][PbNetProtoMember(3)] public long IsLighted { get; set; }
}

[ProtoPackable]
[ProtoContract]
public partial class GiftMaterialSnapShot
{
    [LgrProtoMember(1)][PbNetProtoMember(1)] public string ImgBasic { get; set; }
    [LgrProtoMember(2)][PbNetProtoMember(2)] public string Webp { get; set; }
    [LgrProtoMember(3)][PbNetProtoMember(3)] public long EffectId { get; set; }
    [LgrProtoMember(4)][PbNetProtoMember(4)] public long HasImagedGift { get; set; }
    [LgrProtoMember(5)][PbNetProtoMember(5)] public string ImgGif { get; set; }
}

public enum HaveMedalType
{
    MedalCommon = 0,
    MedalGroup = 1,
}

[ProtoPackable]
[ProtoContract]
public partial class RiskCtrlInfo
{
    [LgrProtoMember(1)][PbNetProtoMember(1)] public string Name { get; set; }
    [LgrProtoMember(2)][PbNetProtoMember(2)] public string Face { get; set; }
}

[ProtoPackable]
[ProtoContract]
public partial class AccountInfo
{
    [LgrProtoMember(1)][PbNetProtoMember(1)] public string Name { get; set; }
    [LgrProtoMember(2)][PbNetProtoMember(2)] public string Face { get; set; }
}

[ProtoPackable]
[ProtoContract]
public partial class OfficialInfo
{
    [LgrProtoMember(1)][PbNetProtoMember(1)] public int Role { get; set; }
    [LgrProtoMember(2)][PbNetProtoMember(2)] public string Title { get; set; }
    [LgrProtoMember(3)][PbNetProtoMember(3)] public string Desc { get; set; }
    [LgrProtoMember(4)][PbNetProtoMember(4)] public int Type { get; set; }
}
