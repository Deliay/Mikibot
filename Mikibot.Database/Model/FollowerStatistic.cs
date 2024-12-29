using System.ComponentModel.DataAnnotations;

namespace Mikibot.Database.Model;

public class FollowerStatistic
{
    [Key]
    public long Id { get; set; }
    public string Bid { get; set; }
    public int FollowerCount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}