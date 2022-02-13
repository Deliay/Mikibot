using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mikibot.Analyze.Database.Model
{
    public class LiveBuyGuardLog
    {
        [Key]
        public int Id { get; set; }
        public int Bid { get; set; }
        public int Uid { get; set; }
        public string UserName { get; set; }
        public string GuardLevel { get; set; }
        public int Price { get; set; }
        public string GiftName { get; set; }
        public DateTimeOffset BoughtAt { get; set; }
        public DateTimeOffset CreatedAt { get; set; }

    }
}
