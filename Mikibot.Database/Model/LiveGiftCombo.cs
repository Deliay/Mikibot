using System.ComponentModel.DataAnnotations;

namespace Mikibot.Database.Model;

public class LiveGiftCombo
{
    [Key]
    public long Id { get; set; }
    public string Bid { get; set; }
    public string Uid { get; set; }
    public string UserName { get; set; }
    public string ComboId { get; set; }
    public string Action { get; set; }
    public int ComboNum { get; set; }
    public string GiftName { get; set; }
    public int TotalCoin { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}