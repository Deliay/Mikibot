using Microsoft.Extensions.Logging;

namespace Mikibot.Analyze.Service.Ai;

public sealed class DeepSeekChatService(ILogger<DeepSeekChatService> logger)
    : AbstractOpenAiLikeChatService<DeepSeekChatService>(logger, new Uri(DeepSeekHost), DeepSeekToken)
{
    private static readonly string DeepSeekToken = Environment
        .GetEnvironmentVariable("DEEPSEEK_TOKEN")
        ?? throw new ArgumentException("DeepSeek API token not configured");
    
    private static readonly string DeepSeekHost = Environment
        .GetEnvironmentVariable("DEEPSEEK_HOST")
        ?? throw new ArgumentException("DeepSeek API token not configured");
    
    public override string Id => "deepseek";
}
