using System.ComponentModel.DataAnnotations;

namespace Mikibot.Database.Model;

public class ChatbotContext
{
    [Key] public long Id { get; set; }
    
    public string GroupId { get; set; }
    
    public string Context { get; set; }
}
