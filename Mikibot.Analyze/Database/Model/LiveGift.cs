using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mikibot.Analyze.Database.Model
{
    public class LiveGift
    {
        [Key]
        public int Id { get; set; }
        public int Bid { get; set; }
        public int Uid { get; set; }
        public string UserName { get; set; }
        public string ComboId { get; set; }
        public string CoinType { get; set; }
        public string Action { get; set; }
        public int DiscountPrice { get; set; }
        public string GiftName { get; set; }
        public DateTimeOffset SentAt { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}
