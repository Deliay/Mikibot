using Mirai.Net.Data.Messages;
using Mirai.Net.Data.Messages.Receivers;
using Mirai.Net.Data.Shared;

namespace Mikibot.Analyze.MiraiHttp
{
    public interface IMiraiService
    {
        ValueTask Run();
        ValueTask SendMessageToAllGroup(CancellationToken token, params MessageBase[] messages);
        ValueTask SendMessageToGroup(Group group, CancellationToken token, params MessageBase[] messages);
        ValueTask SendMessageToSliceManGroup(CancellationToken token, params MessageBase[] messages);
        public void SubscribeMessage(Action<GroupMessageReceiver> next, CancellationToken token);
        ValueTask SendMessageToSomeGroup(HashSet<string> groupIds, CancellationToken token, params MessageBase[] messages);
    }
}