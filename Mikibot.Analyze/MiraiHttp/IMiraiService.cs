using Mirai.Net.Data.Messages;

namespace Mikibot.Analyze.MiraiHttp
{
    public interface IMiraiService
    {
        ValueTask Run();
        ValueTask SendMessageToAllGroup(CancellationToken token, params MessageBase[] messages);
    }
}