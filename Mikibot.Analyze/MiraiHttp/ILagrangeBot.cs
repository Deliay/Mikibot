namespace Mikibot.Analyze.MiraiHttp;

public interface ILagrangeBot
{
    public ValueTask<bool> ReactionGroupMessageAsync(string groupId, string messageId, string emotionId, 
        bool isAdd = true,
        CancellationToken cancellationToken = default);
}