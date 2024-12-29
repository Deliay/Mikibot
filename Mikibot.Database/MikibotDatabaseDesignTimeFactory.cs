using Microsoft.EntityFrameworkCore.Design;

namespace Mikibot.Database;

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