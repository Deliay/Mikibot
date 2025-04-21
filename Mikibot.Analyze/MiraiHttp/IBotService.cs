using Mirai.Net.Data.Messages;
using Mirai.Net.Data.Messages.Receivers;
using Mirai.Net.Data.Shared;

namespace Mikibot.Analyze.MiraiHttp;

public interface IBotService
{
    HttpClient HttpClient { get; }   
    ValueTask Run(CancellationToken cancellationToken = default);
    public string UserId { get; }
    
    public void SubscribeMessage(Action<GroupMessageReceiver> next, CancellationToken token);
    
    ValueTask SendMessageToGroup(Group group, CancellationToken token, params MessageBase[] messages);
        
    ValueTask<Dictionary<string, string>> SendMessageToSomeGroup(HashSet<string> groupIds, CancellationToken token, params MessageBase[] messages);
}