using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Mikibot.Analyze.Service.Ai;

public record Choice(Message message);
public record Response(List<Choice> choices);
public abstract class AbstractOpenAiLikeChatService<T>(ILogger<T> logger, Uri baseUrl, string token, string? overrideModel = null)
    : IBotChatService
where T : AbstractOpenAiLikeChatService<T>
{
    private static readonly string ServiceName = typeof(T).Name;
    private readonly HttpClient _httpClient = new()
    {
        DefaultRequestHeaders =
        {
            { "Accept", "application/json" },
            { "Authorization", $"Bearer {token}" }
        },
        Timeout = TimeSpan.FromSeconds(60),
    };

    public abstract string Id { get; }

    private static readonly JsonSerializerOptions Options = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };
    
    public async ValueTask<List<GroupChatResponse>> ChatAsync(Chat chat, CancellationToken cancellationToken = default)
    {
        if (overrideModel != null) chat = chat with { model = overrideModel };
        
        logger.LogInformation("{} will call service at {} with argument: {}", ServiceName, baseUrl,
            JsonSerializer.Serialize(chat, Options));
        
        var res = await _httpClient.PostAsJsonAsync(baseUrl, chat, cancellationToken);

        if (!res.IsSuccessStatusCode)
        {
            var message = await res.Content.ReadAsStringAsync(cancellationToken);
            logger.LogWarning("{} service call failed: {}, message: {}", 
                ServiceName, res.StatusCode, message);
            return [];
        }

        var requestResponse = await res.Content.ReadAsStringAsync(cancellationToken);
        logger.LogInformation("{} service response: {}", ServiceName,
            JsonSerializer.Serialize(requestResponse, Options));
        
        var data = JsonSerializer.Deserialize<Response>(requestResponse);

        if (data is null || data.choices.Count == 0)
        {
            logger.LogWarning("{} returned an empty response", ServiceName);
            return [];
        }

        var response = data.choices[0];

        var content = response.message.content;

        try
        {
            logger.LogInformation("{} bot: {}", ServiceName, content);

            content = content.Replace("```json", "");
            content = content.Replace("```", "");

            return JsonSerializer.Deserialize<List<GroupChatResponse>>(content) ?? [];
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An exception thrown when submitting to {}", ServiceName);
            return [];
        }
    }
}