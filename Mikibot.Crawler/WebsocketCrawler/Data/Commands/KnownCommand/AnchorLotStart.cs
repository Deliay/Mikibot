using System.Text.Json.Serialization;

namespace Mikibot.Crawler.WebsocketCrawler.Data.Commands.KnownCommand;

public struct AnchorLotStart
{
    [JsonPropertyName("asset_icon")]
    public string AssetIconUrl { get; set; }

    [JsonPropertyName("award_image")]
    public string AwardImageUrl { get; set; }

    [JsonPropertyName("award_name")]
    public string AwardName { get; set; }

    [JsonPropertyName("award_num")]
    public int AwardNum { get; set; }

    [JsonPropertyName("cur_gift_num")]
    public int CurGiftNum { get; set; }

    [JsonPropertyName("current_time")]
    public int CurrentTime { get; set; }

    [JsonPropertyName("danmu")]
    public string Danmu { get; set; }

    [JsonPropertyName("gift_id")]
    public int GiftId { get; set; }

    [JsonPropertyName("gift_name")]
    public string GiftName { get; set; }

    [JsonPropertyName("gift_num")]
    public int GiftNum { get; set; }

    [JsonPropertyName("gift_price")]
    public int GiftPrice { get; set; }

    [JsonPropertyName("goaway_time")]
    public int GoawayTime { get; set; }

    [JsonPropertyName("goods_id")]
    public int GoodsId { get; set; }

    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("is_broadcast")]
    public int IsBroadcast { get; set; }

    [JsonPropertyName("join_type")]
    public int JoinType { get; set; }

    [JsonPropertyName("lot_status")]
    public int LotStatus { get; set; }

    [JsonPropertyName("max_time")]
    public int MaxTime { get; set; }

    [JsonPropertyName("require_text")]
    public string RequireText { get; set; }

    [JsonPropertyName("require_type")]
    public int RequireType { get; set; }

    [JsonPropertyName("require_value")]
    public int RequireValue { get; set; }

    [JsonPropertyName("room_id")]
    public int RoomId { get; set; }

    [JsonPropertyName("send_gift_ensure")]
    public int SendGiftEnsure { get; set; }

    [JsonPropertyName("show_panel")]
    public int ShowPanel { get; set; }

    [JsonPropertyName("start_dont_popup")]
    public int StartDontPopup { get; set; }

    [JsonPropertyName("status")]
    public int Status { get; set; }

    [JsonPropertyName("time")]
    public int Time { get; set; }

    [JsonPropertyName("url")]
    public string LotUrl { get; set; }

    [JsonPropertyName("web_url")]
    public string WebUrl { get; set; }
}
