using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Mikibot.Database.Model;

[Index(nameof(Bid))]
public class StatisticReportLog
{
    [Key]
    public long Id { get; set; }
    public string Bid { get; set; }
    public string ReportIdentity { get; set; }
    public DateTimeOffset ReportedAt { get; set; }
}