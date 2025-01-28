using Mirai.Net.Data.Messages;
using Mirai.Net.Data.Messages.Receivers;
using Mirai.Net.Data.Shared;

namespace Mikibot.Analyze.MiraiHttp;

public interface IMiraiService
{
    ValueTask Run();
    ValueTask SendMessageToGroup(Group group, CancellationToken token, params MessageBase[] messages);
    public void SubscribeMessage(Action<GroupMessageReceiver> next, CancellationToken token);
        
    public string UserId { get; }
        
    ValueTask SendMessageToSomeGroup(HashSet<string> groupIds, CancellationToken token, params MessageBase[] messages);
    [Obsolete("Will be removed in future version")]
    ValueTask SendMessageToSliceManGroup(CancellationToken token, params MessageBase[] messages);
    [Obsolete("Will be removed in future version")]
    ValueTask SendMessageToAllGroup(CancellationToken token, params MessageBase[] messages);
}