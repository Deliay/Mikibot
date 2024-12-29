using System.ComponentModel.DataAnnotations;

namespace Mikibot.Database.Model;

public class SubscriptionLiveStart
{
    [Key]
    public long Id { get; set; }
    
    public required string GroupId { get; set; }
    
    public required string UserId { get; set; }
    public string RoomId { get; set; }
    
    public required bool EnabledFansTrendingStatistics { get; set; }
}
