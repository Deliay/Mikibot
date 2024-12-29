using System.ComponentModel.DataAnnotations;

namespace Mikibot.Database.Model;

public class LiveSuperChat
{
    [Key]
    public long Id { get; set; }
    public string Message { get; set; }
    public int Price { get; set; }
    public DateTimeOffset SentAt { get; set; }
    public string Bid { get; set; }
    public string Uid { get; set; }
    public string UserName { get; set; }
    public string MedalUserId { get; set; }
    public string MedalName { get; set; }
    public int MedalLevel { get; set; }
    public int MedalGuardLevel { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

}