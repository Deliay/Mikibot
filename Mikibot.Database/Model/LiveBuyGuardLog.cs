using System.ComponentModel.DataAnnotations;

namespace Mikibot.Database.Model;

public class LiveBuyGuardLog
{
    [Key]
    public long Id { get; set; }
    public string Bid { get; set; }
    public string Uid { get; set; }
    public string UserName { get; set; }
    public string GuardLevel { get; set; }
    public int Price { get; set; }
    public string GiftName { get; set; }
    public DateTimeOffset BoughtAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

}