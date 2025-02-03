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

public record Chat(List<Message> messages,
    string model = "deepseek-chat",
    float temperature = 1.3f,
    bool search_enabled = true);

public record GroupChatResponse(int score, string topic, string reply);


public interface IBotChatService
{
    public ValueTask<List<GroupChatResponse>> ChatAsync(Chat chat, CancellationToken cancellationToken = default);
}
