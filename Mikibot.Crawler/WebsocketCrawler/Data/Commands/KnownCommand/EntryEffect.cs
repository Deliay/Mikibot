using System.Text.Json.Serialization;
using Mikibot.Crawler.WebsocketCrawler.Data.Commands.Utils;

namespace Mikibot.Crawler.WebsocketCrawler.Data.Commands.KnownCommand;

public struct EntryEffect
{
    [JsonPropertyName("uid")]
    public long UserId { get; set; }
    [JsonPropertyName("privilege_type")]
    public int GuardLevel { get; set; }
    [JsonPropertyName("copy_writing")]
    public string CopyWriting { get; set; }
    [JsonPropertyName("trigger_time")]
    [JsonConverter(typeof(UnixNanoSecondOffsetConverter))]
    public DateTimeOffset EnteredAt { get; set; }
}