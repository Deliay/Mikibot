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

public record GroupChatResponse(string messageId, int score, string topic, string reply, string? imageUrl);


public interface IBotChatService
{
    public string Id { get; }
    
    public ValueTask<List<GroupChatResponse>> ChatAsync(Chat chat, CancellationToken cancellationToken = default);
}
