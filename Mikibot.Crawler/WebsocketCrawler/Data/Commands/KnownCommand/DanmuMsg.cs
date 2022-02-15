using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Mikibot.Crawler.WebsocketCrawler.Data.Commands
{
    [JsonConverter(typeof(DanmuMsgJsonConverter))]
    public struct DanmuMsg
    {
        public string Msg { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string FansTag { get; set; }
        public int FansLevel { get; set; }
        public int FansTagUserId { get; set; }
        public string FansTagUserName { get; set; }
        public DateTimeOffset SentAt { get; set; }
    }
}
