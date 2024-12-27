using System.Text.Json.Serialization;

namespace Mikibot.Crawler.Http.Bilibili.Model;

public struct Emoticons
{
    [JsonPropertyName("data")]
    public List<Emoticon> Data { get; set; }

    [JsonPropertyName("fans_brand")]
    public int FansBrand { get; set; }

    [JsonPropertyName("purchase_url")]
    public string PurchaseUrl { get; set; }
}