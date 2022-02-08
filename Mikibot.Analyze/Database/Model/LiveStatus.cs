using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mikibot.Mirai.Database.Model
{
    public class LiveStatus
    {
        [Key]
        public int Id { get; set; }
        public string Bid { get; set; }
        public int Status { get; set; }
        public string Title { get; set; }
        public string Cover { get; set; }
        public int FollowerCount { get; set; }
        public bool Notified { get; set; }
        public DateTimeOffset NotifiedAt { get; set; }
        public DateTimeOffset StatusChangedAt { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
    }
}
