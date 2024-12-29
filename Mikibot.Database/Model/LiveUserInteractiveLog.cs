using System.ComponentModel.DataAnnotations;

namespace Mikibot.Database.Model;

public class LiveUserInteractiveLog
{
    [Key]
    public long Id { get; set; }
    public string Bid { get; set; }
    public string UserId { get; set; }
    public string UserName { get; set; }
    public DateTimeOffset InteractedAt { get; set; }
    public int GuardLevel { get; set; }
    public int MedalLevel { get; set; }
    public string MedalName { get; set; }
    public string FansTagUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}