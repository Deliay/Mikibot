using System.Text.Json.Serialization;

namespace Mikibot.Crawler.Http.Bilibili.Model;

public struct VideoInfo
{
    public struct OwnerInfo
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
    }

    [JsonPropertyName("title")]
    public string Title { get; set; }

    [JsonPropertyName("desc")]
    public string Description { get; set; }

    [JsonPropertyName("pic")]
    public string CoverUrl { get; set; }

    [JsonPropertyName("bvid")]
    public string BvId { get; set; }

    [JsonPropertyName("owner")]
    public OwnerInfo Owner { get; set; }
}