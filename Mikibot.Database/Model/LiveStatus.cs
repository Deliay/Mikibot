using System.ComponentModel.DataAnnotations;

namespace Mikibot.Database.Model;

public class LiveStatus
{
    [Key]
    public long Id { get; set; }
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