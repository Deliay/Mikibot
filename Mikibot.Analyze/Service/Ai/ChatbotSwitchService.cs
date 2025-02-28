using Microsoft.Extensions.Logging;

namespace Mikibot.Analyze.Service.Ai;

public class ChatbotSwitchService
{
    
    private readonly Dictionary<string, IBotChatService> chatbotMap;

    public ChatbotSwitchService(IEnumerable<IBotChatService> chatbots, ILogger<ChatbotSwitchService> logger)
    {
        chatbotMap = chatbots.ToDictionary(x => x.Id, x => x);
        
        Chatbot = chatbotMap.Values
            .OrderBy(c => c.Id)
            .FirstOrDefault() ?? throw new ArgumentNullException(nameof(chatbots));
        
        logger.LogInformation("Default chatbot service: {}", Chatbot.Id);
    }

    public IBotChatService Chatbot { get; private set; }


    public bool UpdateChatbot(string id)
    {
        if (!chatbotMap.TryGetValue(id, out var botChatService)) return false;
        
        Chatbot = botChatService;
        return true;
    }
}