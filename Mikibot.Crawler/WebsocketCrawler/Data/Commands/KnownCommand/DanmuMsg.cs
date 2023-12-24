using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Mikibot.Crawler.WebsocketCrawler.Data.Commands.KnownCommand
{
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
}
