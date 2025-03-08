using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using OllamaSharp;
using OllamaSharp.Models;

namespace Mikibot.Analyze.Service.Ai;

public class OllamaChatService : IBotChatService
{
    public ILogger<OllamaChatService> Logger { get; }
    private readonly OllamaApiClient ollamaClient;
    private readonly string ollamaModel;

    public OllamaChatService(ILogger<OllamaChatService> logger)
    {
        Logger = logger;
        var ollamaEndpoint = new Uri(Environment.GetEnvironmentVariable("OLLAMA_ENDPOINT")
                                     ?? "http://localhost:11434");

        this.ollamaModel = Environment.GetEnvironmentVariable("OLLAMA_MODEL")
                          ?? "deepseek-r1:14b";

        var client = new HttpClient();
        client.Timeout = TimeSpan.FromMinutes(5);
        client.BaseAddress = ollamaEndpoint;
        ollamaClient = new OllamaApiClient(client, ollamaModel);
    }

    public string Id => "ollama";

    public async ValueTask<Response?> LlmChatAsync(Chat chat, CancellationToken cancellationToken = default)
    {
        var result = await ollamaClient.GenerateAsync(new GenerateRequest()
        {
            Model = ollamaModel,
            Prompt = chat.ToPlainText(),
            Stream = false,
            Options = new RequestOptions()
            {
                Temperature = chat.temperature,
            },
        }, cancellationToken).ToListAsync(cancellationToken);

        var choices = result
            .Where(item => item is not null)
            .Select(item => new Choice(new Message(ChatRole.Assistant, item!.Response)))
            .ToList();

        return new Response(choices);
    }

    public async ValueTask<List<GroupChatResponse>> ChatAsync(Chat chat, CancellationToken cancellationToken = default)
    {
        var result = await ollamaClient.GenerateAsync(new GenerateRequest()
        {
            Model = ollamaModel,
            Prompt = chat.ToPlainText(),
            Stream = false,
            Options = new RequestOptions()
            {
                Temperature = chat.temperature,
            },
        }, cancellationToken).ToListAsync(cancellationToken);

        var response = result.FirstOrDefault();
        
        var content = response?.Response;
        if (content is null) return [];
        
        Logger.LogInformation("Ollama: {}", content);
        try
        {
            var thinkTagIndex = content.IndexOf("</think>", StringComparison.InvariantCulture);
            if (thinkTagIndex > 0)
            {
                thinkTagIndex += "</think>".Length + 1;
                content = content[thinkTagIndex..];
            }
            
            content = content.Replace("```json", "");
            content = content.Replace("```", "");

            if (content.StartsWith('{'))
            {
                return [JsonSerializer.Deserialize<GroupChatResponse>(content)!];
            }
            
            return JsonSerializer.Deserialize<List<GroupChatResponse>>(content ?? "") ?? [];
        }
        catch
        {
            return [];
        }
    }
}