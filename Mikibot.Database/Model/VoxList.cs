﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mikibot.Database.Model
{
    public class VoxList
    {
        [Key]
        public int Id { get; set; }

        public string Bid { get; set; }

        public string Name { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
    }
}
