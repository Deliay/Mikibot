﻿using System.ComponentModel.DataAnnotations;

namespace Mikibot.Database.Model;

public class LiveGuardEnterLog
{
    [Key]
    public int Id { get; set; }
    public int Bid { get; set; }
    public int UserId { get; set; }
    public int GuardLevel { get; set; }
    public string CopyWriting { get; set; }
    public DateTimeOffset EnteredAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}