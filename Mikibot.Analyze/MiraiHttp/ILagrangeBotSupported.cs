namespace Mikibot.Analyze.MiraiHttp;

public interface ILagrangeBotSupported
{
    public ValueTask<bool> ReactionGroupMessageAsync(string groupId, string messageId, string emotionId, 
        bool isAdd = true,
        CancellationToken cancellationToken = default);
}