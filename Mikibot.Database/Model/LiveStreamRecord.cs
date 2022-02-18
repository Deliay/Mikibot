using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mikibot.Database.Model
{
    public class LiveStreamRecord
    {
        [Key]
        public int Id { get; set; }
        public int Bid { get; set; }
        public int Duration { get; set; }
        public string LocalFileName { get; set; }
        public bool Reserve { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset RecordStoppedAt { get; set; }
    }
}
