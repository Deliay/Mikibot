using Microsoft.EntityFrameworkCore;
using Mikibot.Database;
using Mikibot.Database.Model;

namespace Mikibot.Analyze.Generic;

public class PermissionService(MikibotDatabaseContext db)
{
    public async ValueTask<bool> HasPermission(string role, string userId, string action, CancellationToken cancellationToken = default)
    {

        return await db.Permissions.AnyAsync(
            x => x.Role == role && x.UserId == userId && x.Action == action, cancellationToken);
    }
    
    public ValueTask<bool> IsBotOperator(string userId, CancellationToken cancellationToken = default)
        => HasPermission(User, userId, BotOperator, cancellationToken);

    public const string User = "User";
    public const string Group = "Group";
    public const string BotOperator = "BotOperator";
    
    public async ValueTask<bool> GrantPermission(string @operator, string role, string userId, string action,
        CancellationToken cancellationToken = default)
    {
        if (!await HasPermission(User, @operator, BotOperator, cancellationToken)) return false;
        
        await db.Permissions.AddAsync(new Permission
        {
            Action = action,
            Role = role,
            UserId = userId,
        }, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);
        return true;

    }
}