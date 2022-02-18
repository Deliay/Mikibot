using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mikibot.Analyze.Database.Model
{
    public class LiveSuperChat
    {
        [Key]
        public int Id { get; set; }
        public string Message { get; set; }
        public int Price { get; set; }
        public DateTimeOffset SentAt { get; set; }
        public int Bid { get; set; }
        public int Uid { get; set; }
        public string UserName { get; set; }
        public int MedalUserId { get; set; }
        public string MedalName { get; set; }
        public int MedalLevel { get; set; }
        public int MedalGuardLevel { get; set; }
        public DateTimeOffset CreatedAt { get; set; }

    }
}
