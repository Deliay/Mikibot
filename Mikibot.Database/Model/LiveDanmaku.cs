using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mikibot.Analyze.Database.Model
{
    public class LiveDanmaku
    {
        [Key]
        public int Id { get; set; }
        public int Bid { get; set; }
        public int UserId { get; set; }
        public string Msg { get; set; }
        public string UserName { get; set; }
        public string FansTag { get; set; }
        public int FansLevel { get; set; }
        public int FansTagUserId { get; set; }
        public string FansTagUserName { get; set; }
        public DateTimeOffset SentAt { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}
