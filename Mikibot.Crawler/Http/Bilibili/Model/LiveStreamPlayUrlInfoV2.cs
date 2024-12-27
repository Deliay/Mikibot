using System.Text.Json.Serialization;

namespace Mikibot.Crawler.Http.Bilibili.Model;

public class LiveStreamPlayUrlInfoV2
{
    [JsonPropertyName("playurl")]
    public LiveStreamPlayUrlV2 PlayUrl { get; set; }
}