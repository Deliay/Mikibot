using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mikibot.Analyze.Database.Model
{
    [Index(nameof(Bid))]
    public class StatisticReportLog
    {
        [Key]
        public int Id { get; set; }
        public string Bid { get; set; }
        public string ReportIdentity { get; set; }
        public DateTimeOffset ReportedAt { get; set; }
    }
}
