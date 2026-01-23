using System.Text.Json.Serialization;

namespace Mikibot.Crawler.Http.Bilibili.Model;

public struct WbiInfo
{
    [JsonPropertyName("img_url")]
    public string ImgUrl { get; set; }

    [JsonPropertyName("sub_url")]
    public string SubUrl { get; set; }
}

public struct NavInfo
{
    [JsonPropertyName("mid")]
    public long Mid { get; set; }

    [JsonPropertyName("uname")]
    public string Name { get; set; }
    
    [JsonPropertyName("wbi_img")]
    public WbiInfo WbiInfo { get; set; }
}