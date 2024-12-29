using System.ComponentModel.DataAnnotations;

namespace Mikibot.Database.Model;

public class VoxList
{
    [Key]
    public int Id { get; set; }

    public string Bid { get; set; }

    public string Name { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}