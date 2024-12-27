using System.Text.Json.Serialization;
using Mikibot.Crawler.WebsocketCrawler.Data.Commands.Utils;

namespace Mikibot.Crawler.WebsocketCrawler.Data.Commands.KnownCommand;

[JsonConverter(typeof(DanmuMsgJsonConverter))]
public struct DanmuMsg
{
    public string Msg { get; set; }
    public long UserId { get; set; }
    public string UserName { get; set; }
    public string FansTag { get; set; }
    public int FansLevel { get; set; }
    public long FansTagUserId { get; set; }
    public string FansTagUserName { get; set; }
    public DateTimeOffset SentAt { get; set; }
    public string MemeUrl { get; set; }
    public string HexColor { get; set; }
}