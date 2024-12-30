using System.ComponentModel.DataAnnotations;

namespace Mikibot.Database.Model;

public class RandomFood
{
    [Key]
    public long Id { get; set; }

    public required string Name { get; set; }
}