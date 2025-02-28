﻿using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Mikibot.Analyze.Service.Ai;

public record Choice(Message message);
public record Response(List<Choice> choices);
public abstract class OpenAiChatService<T>(ILogger<T> logger, Uri baseUrl, string token, string? overrideModel = null)
    : IBotChatService
where T : OpenAiChatService<T>
{
    private static readonly string ServiceName = typeof(T).Name;
    private readonly HttpClient _httpClient = new()
    {
        DefaultRequestHeaders =
        {
            { "Accept", "application/json" },
            { "Authorization", $"Bearer {token}" }
        },
        BaseAddress = baseUrl,
        Timeout = TimeSpan.FromSeconds(60),
    };

    public abstract string Id { get; }

    public async ValueTask<List<GroupChatResponse>> ChatAsync(Chat chat, CancellationToken cancellationToken = default)
    {
        if (overrideModel != null) chat = chat with { model = overrideModel };
        
        var res = await _httpClient.PostAsJsonAsync("/chat/completions", chat, cancellationToken);

        if (!res.IsSuccessStatusCode)
        {
            var message = await res.Content.ReadAsStringAsync(cancellationToken);
            logger.LogWarning("{} service call failed: {}, message: {}", 
                ServiceName, res.StatusCode, message);
            return [];
        }

        var data = await res.Content.ReadFromJsonAsync<Response>(cancellationToken);

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