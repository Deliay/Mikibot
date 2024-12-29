using System.ComponentModel.DataAnnotations;

namespace Mikibot.Database.Model;

public class LiveStreamRecord
{
    [Key]
    public long Id { get; set; }
    public string Bid { get; set; }
    public int Duration { get; set; }
    public string LocalFileName { get; set; }
    public bool Reserve { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset RecordStoppedAt { get; set; }
}