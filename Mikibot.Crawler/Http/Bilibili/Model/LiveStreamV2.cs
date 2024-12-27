using System.Text.Json.Serialization;

namespace Mikibot.Crawler.Http.Bilibili.Model;

public struct LiveStreamV2
{
    [JsonPropertyName("protocol_name")]
    public string ProtocolName { get; set; }

    [JsonPropertyName("format")]
    public List<LiveStreamFormatV2> Formats { get; set; }
}