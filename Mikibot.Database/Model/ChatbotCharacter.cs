using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mikibot.Database.Model;

public class ChatbotCharacter
{
    [Key] public long Id { get; set; }
    public string GroupId { get; set; }
    public string Description { get; set; }
}
