using System.ComponentModel.DataAnnotations;

namespace Mikibot.Database.Model;

public class SubscriptionFansTrends
{
    [Key]
    public long Id { get; set; }
    
    public required string GroupId { get; set; }
    public required string UserId { get; set; }
    
    public required int TargetFansCount { get; set; }
}
