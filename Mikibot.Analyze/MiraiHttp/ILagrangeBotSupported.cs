namespace Mikibot.Analyze.MiraiHttp;

public interface ILagrangeBotSupported
{
    public ValueTask<bool> ReactionGroupMessageAsync(string groupId, string userId, string messageId, 
        bool isAdd = true,
        CancellationToken cancellationToken = default);
}