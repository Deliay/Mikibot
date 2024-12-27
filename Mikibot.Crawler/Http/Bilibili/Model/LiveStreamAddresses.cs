using System.Text.Json.Serialization;

namespace Mikibot.Crawler.Http.Bilibili.Model;

public struct LiveStreamAddresses
{
    [JsonPropertyName("durl")]
    public List<LiveStreamAddress> Urls { get; set; }
}