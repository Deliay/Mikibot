using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mikibot.Analyze.Database.Model
{
    public class FollowerStatistic
    {
        [Key]
        public int Id { get; set; }
        public string Bid { get; set; }
        public int FollowerCount { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}
