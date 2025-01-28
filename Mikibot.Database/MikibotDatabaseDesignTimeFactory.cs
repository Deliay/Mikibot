using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Mikibot.Database;

public class MikibotDatabaseDesignTimeFactory : IDesignTimeDbContextFactory<MikibotDatabaseContext>
{
    public MikibotDatabaseContext CreateDbContext(string[] args)
    {
        return new MikibotDatabaseContext(new MySqlConfiguration()
        {
            Host = "10.100.0.4",
            Database = "mikibot",
            Port = 3306,
            User = "zero_apps",
            Password = "1",
        });
    }
}