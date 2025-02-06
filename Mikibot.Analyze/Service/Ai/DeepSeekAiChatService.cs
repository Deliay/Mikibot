using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Mikibot.Analyze.Service.Ai;

public record Choice(Message message);
public record Response(List<Choice> choices);
public class DeepSeekAiChatService : IBotChatService
{
    public ILogger<DeepSeekAiChatService> Logger { get; }
    private HttpClient _httpClient;

    public DeepSeekAiChatService(ILogger<DeepSeekAiChatService> logger)
    {
        Logger = logger;

        _httpClient = new HttpClient();
        var token = Environment.GetEnvironmentVariable("DEEPSEEK_TOKEN")
                    ?? throw new ArgumentException("DeepSeek API token not configured");
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
        _httpClient.Timeout = TimeSpan.FromSeconds(60);
    }

    public string Id => "deepseek";

    public async ValueTask<List<GroupChatResponse>> ChatAsync(Chat chat, CancellationToken cancellationToken = default)
    {
        var res = await _httpClient.PostAsJsonAsync("https://api.deepseek.com/chat/completions", chat, cancellationToken);

        if (!res.IsSuccessStatusCode)
        {
            var message = await res.Content.ReadAsStringAsync(cancellationToken);
            Logger.LogWarning("Deepseek service call failed: {}, message: {}", 
                res.StatusCode, message);
            return [];
        }

        var data = await res.Content.ReadFromJsonAsync<Response>(cancellationToken);

        if (data is null || data.choices.Count == 0)
        {
            Logger.LogWarning("Deepseek returned an empty response");
            return [];
        }

        var response = data.choices[0];

        var content = response.message.content;

        try
        {
            Logger.LogInformation("Deekseep bot: {}", content);

            content = content.Replace("```json", "");
            content = content.Replace("```", "");

            return JsonSerializer.Deserialize<List<GroupChatResponse>>(content) ?? [];
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "An exception thrown when submitting to deepseek");
            return [];
        }
    }
}