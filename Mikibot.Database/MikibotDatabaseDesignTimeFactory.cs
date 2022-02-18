using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mikibot.Analyze.Database
{
    public class MikibotDatabaseDesignTimeFactory : IDesignTimeDbContextFactory<MikibotDatabaseContext>
    {
        public MikibotDatabaseContext CreateDbContext(string[] args)
        {
            return new MikibotDatabaseContext(new MySqlConfiguration()
            {
                Host = "localhost",
                Database = "mikibot",
                Port = 3306,
                User = "root",
            });
        }
    }
}
