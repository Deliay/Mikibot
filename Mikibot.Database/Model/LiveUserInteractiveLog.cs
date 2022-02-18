using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mikibot.Database.Model
{
    public class LiveUserInteractiveLog
    {
        [Key]
        public int Id { get; set; }
        public int Bid { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; }
        public DateTimeOffset InteractedAt { get; set; }
        public int GuardLevel { get; set; }
        public int MedalLevel { get; set; }
        public string MedalName { get; set; }
        public int FansTagUserId { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}
