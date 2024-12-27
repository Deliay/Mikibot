using System.Text.Json.Serialization;

namespace Mikibot.Crawler.Http.Bilibili.Model;

public struct LiveStreamPlayUrlV2
{
    [JsonPropertyName("stream")]
    public List<LiveStreamV2> Streams { get; set; }
}