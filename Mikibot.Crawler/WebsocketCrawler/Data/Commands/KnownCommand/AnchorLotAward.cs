using System.Text.Json.Serialization;

namespace Mikibot.Crawler.WebsocketCrawler.Data.Commands.KnownCommand;

public struct AnchorLotAward
{

    [JsonPropertyName("award_dont_popup")]
    public byte AwardDontPopup { get; set; }

    [JsonPropertyName("award_image")]
    public string AwardImage { get; set; }

    [JsonPropertyName("award_name")]
    public string AwardName { get; set; }

    [JsonPropertyName("award_num")]
    public int AwardNum { get; set; }

    [JsonPropertyName("award_users")]
    public IEnumerable<AwardUser> AwardUsers { get; set; }

    [JsonPropertyName("id")]
    public int LotId { get; set; }

    [JsonPropertyName("lot_status")]
    public int LotStatus { get; set; }

    [JsonPropertyName("url")]
    public string LotUrl { get; set; }

    [JsonPropertyName("web_url")]
    public string WebUrl { get; set; }

    public struct AwardUser
    {
        [JsonPropertyName("uid")]
        public long UserId { get; set; }

        [JsonPropertyName("uname")]
        public string UserName { get; set; }

        [JsonPropertyName("face")]
        public string FaceUrl { get; set; }

        [JsonPropertyName("level")]
        public int Level { get; set; }

        [JsonPropertyName("color")]
        public int Color { get; set; }
    }
}
