using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.AI;

namespace Mikibot.Analyze.Service.Ai;

public record Message(ChatRole role, string content)
{
    public ChatMessage ToChatMessage()
    {
        return new ChatMessage()
        {
            Role = role,
            Text = content,
        };
    }

    public bool TryPluckJsonObjectContent([NotNullWhen(true)]out string? jsonContent)
    {
        return TryPluckClosedContent('{', '}', out jsonContent);
    }
    public bool TryPluckJsonArrayContent([NotNullWhen(true)]out string? jsonContent)
    {
        return TryPluckClosedContent('[', ']', out jsonContent);
    }
    
    private bool TryPluckClosedContent(char leftChar, char rightChar, [NotNullWhen(true)]out string? jsonContent)
    {
        jsonContent = null;
        var left = content.IndexOf(leftChar);
        if (left < 0) return false;
        
        var right = content.LastIndexOf(rightChar);
        if (right < 0) return false;

        jsonContent = content[left..(right + 1)];
        return true;
    }
}

public record Chat(
    List<Message> messages,
    string model = "deepseek-chat",
    float temperature = 1.3f,
    bool search_enabled = true,
    bool stream = false)
{
    public string ToPlainText() => string.Join('\n', messages.Select(m => m.content));
}

public record GroupChatResponse(string? messageId, int score, string topic, string reply, string? imagePrompt);


public interface IBotChatService
{
    public string Id { get; }
    
    public ValueTask<List<GroupChatResponse>> ChatAsync(Chat chat, CancellationToken cancellationToken = default);
    
    
    public ValueTask<Response?> LlmChatAsync(Chat chat, CancellationToken cancellationToken = default);
}
