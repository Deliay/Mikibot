using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mikibot.Database;
using Mikibot.Database.Model;

namespace Mikibot.Analyze.Generic;

public class PermissionService(MikibotDatabaseContext db, ILogger<PermissionService> logger)
{
    public async ValueTask<bool> HasPermission(string role, string userId, string action, CancellationToken cancellationToken = default)
    {
        var result = await db.Permissions.AnyAsync(
            x => x.Role == role && x.UserId == userId && x.Action == action, cancellationToken);
        
        logger.LogInformation("Permission {}:{} action:{}, result: {}", role, userId, action, result);
        return result;
    }

    public ValueTask<bool> IsGroupEnabled(string action, string groupId, CancellationToken cancellationToken = default)
    {
        return HasPermission(Group, groupId, action, cancellationToken);
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