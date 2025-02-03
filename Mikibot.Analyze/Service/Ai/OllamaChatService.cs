using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace Mikibot.Analyze.Service.Ai;

public class OllamaChatService : IBotChatService
{
    public ILogger<OllamaChatClient> Logger { get; }
    private readonly OllamaChatClient ollamaClient;

    public OllamaChatService(ILogger<OllamaChatClient> logger)
    {
        Logger = logger;
        var ollamaEndpoint = new Uri(Environment.GetEnvironmentVariable("OLLAMA_ENDPOINT")
                                     ?? "http://localhost:11434");

        var ollamaModel = Environment.GetEnvironmentVariable("OLLAMA_MODEL")
                          ?? "deepseek-r1:14b";

        this.ollamaClient = new OllamaChatClient(ollamaEndpoint, ollamaModel);
    }
    
    public async ValueTask<List<GroupChatResponse>> ChatAsync(Chat chat, CancellationToken cancellationToken = default)
    {
        var chats = chat.messages.Select(m => m.ToChatMessage()).ToList();
        var result = await ollamaClient.CompleteAsync(chats, new ChatOptions()
        {
            Temperature = chat.temperature,
            ResponseFormat = ChatResponseFormat.Json,
        }, cancellationToken);

        var content = result.Message.Text;
            
        Logger.LogInformation("Ollama: {}", content);

        try
        {
            return JsonSerializer.Deserialize<List<GroupChatResponse>>(content ?? "") ?? [];
        }
        catch
        {
            return [];
        }
    }
}