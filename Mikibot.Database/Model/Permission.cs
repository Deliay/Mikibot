namespace Mikibot.Database.Model;

public class Permission
{
    public long Id { get; set; }

    public required string Action { get; set; }
    
    public required string UserId { get; set; }
    
    public required string Role { get; set; }
}