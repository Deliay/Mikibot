namespace Mikibot.Analyze.Service.Ai;

public class ChatbotSwitchService
{
    
    private readonly Dictionary<string, IBotChatService> chatbotMap;

    public ChatbotSwitchService(IEnumerable<IBotChatService> chatbots)
    {
        chatbotMap = chatbots.ToDictionary(x => x.Id, x => x);
        
        Chatbot = chatbotMap.Values.FirstOrDefault()
            ?? throw new ArgumentNullException(nameof(chatbots));
    }

    public IBotChatService Chatbot { get; private set; }


    public bool UpdateChatbot(string id)
    {
        if (!chatbotMap.TryGetValue(id, out var botChatService)) return false;
        
        Chatbot = botChatService;
        return true;
    }
}