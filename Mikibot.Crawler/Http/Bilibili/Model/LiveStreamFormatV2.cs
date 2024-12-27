using System.Text.Json.Serialization;

namespace Mikibot.Crawler.Http.Bilibili.Model;

public struct LiveStreamFormatV2
{
    [JsonPropertyName("format_name")]
    public string FormatName { get; set; }

    [JsonPropertyName("codec")]
    public List<LiveStreamFormatCodecV2> Codec { get; set; }
}