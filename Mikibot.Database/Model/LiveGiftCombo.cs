using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mikibot.Analyze.Database.Model
{
    public class LiveGiftCombo
    {
        [Key]
        public int Id { get; set; }
        public int Bid { get; set; }
        public int Uid { get; set; }
        public string UserName { get; set; }
        public string ComboId { get; set; }
        public string Action { get; set; }
        public int ComboNum { get; set; }
        public string GiftName { get; set; }
        public int TotalCoin { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}
