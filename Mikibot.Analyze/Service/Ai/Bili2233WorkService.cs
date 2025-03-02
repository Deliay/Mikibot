using Microsoft.Extensions.Logging;

namespace Mikibot.Analyze.Service.Ai;

public sealed class Bili2233WorkService(ILogger<Bili2233WorkService> logger)
    : AbstractOpenAiLikeChatService<Bili2233WorkService>(logger,
        new Uri(Bili2233Host), Bili2233Token, Bili2233Model)
{
    private static readonly string Bili2233Token = Environment
        .GetEnvironmentVariable("BILI2233_TOKEN")
        ?? throw new ArgumentException("DeepSeek API token not configured");
    
    private static readonly string Bili2233Host = Environment
        .GetEnvironmentVariable("BILI2233_HOST")
        ?? throw new ArgumentException("DeepSeek API token not configured");
    
    private static readonly string Bili2233Model = Environment
        .GetEnvironmentVariable("BILI2233_MODEL")
        ?? "gemini-2.0-flash-exp";

    public override string Id => "bili2233";
}
