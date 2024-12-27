using System.Text.Json.Serialization;

namespace Mikibot.Crawler.Http.Bilibili.Model.LiveServer;

public struct LiveServerInfo
{
    public string Host { get; set; }
    public int Port { get; set; }
    [JsonPropertyName("wss_port")]
    public int WssPort { get; set; }
    [JsonPropertyName("ws_port")]
    public int WsPort { get; set; }
}