using Mirai.Net.Data.Messages.Concretes;

namespace Mikibot.Analyze.MiraiHttp;

public static class LagrangeBotExtensions
{
    public static async ValueTask<bool> SendGroupMessageReactionIfSupported(this IBotService bot, string groupId,
        string? messageId, string emotionId, bool isAdd = true, CancellationToken cancellationToken = default)
    {
        if (bot is not ILagrangeBot lagrangeBot) return false;
        
        if (messageId is not null)
        {
            return await lagrangeBot.ReactionGroupMessageAsync(groupId, messageId, emotionId,
                isAdd, cancellationToken);
        }

        return false;
    }
}
