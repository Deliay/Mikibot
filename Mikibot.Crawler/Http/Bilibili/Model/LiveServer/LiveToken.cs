using System.Text.Json.Serialization;

namespace Mikibot.Crawler.Http.Bilibili.Model.LiveServer;

public struct LiveToken
{
    public string Token { get; set; }
    [JsonPropertyName("host_list")]
    public List<LiveServerInfo> Hosts { get; set; }
}