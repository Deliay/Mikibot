using System.ComponentModel.DataAnnotations;

namespace Mikibot.Database.Model;

public class LiveDanmaku
{
    [Key]
    public long Id { get; set; }
    public string Bid { get; set; }
    public string UserId { get; set; }
    public string Msg { get; set; }
    public string UserName { get; set; }
    public string FansTag { get; set; }
    public int FansLevel { get; set; }
    public int FansTagUserId { get; set; }
    public string FansTagUserName { get; set; }
    public DateTimeOffset SentAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}