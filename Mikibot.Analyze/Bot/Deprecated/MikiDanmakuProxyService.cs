using Mikibot.Analyze.MiraiHttp;
using Mikibot.Crawler.WebsocketCrawler.Data.Commands.KnownCommand;
using Mirai.Net.Data.Messages;
using Mirai.Net.Data.Messages.Concretes;

namespace Mikibot.Analyze.Bot;

public class MikiDanmakuProxyService(IMiraiService miraiService)
{
    private IMiraiService MiraiService { get; } = miraiService;

    public async Task HandleDanmaku(DanmuMsg msg)
    {
        if (msg.UserId == 477317922)
        {
            if (msg.MemeUrl == string.Empty)
            {
                await MiraiService.SendMessageToAllGroup(default, new MessageBase[]
                {
                    new PlainMessage()
                    {
                        Text = $"(直播弹幕) {msg.UserName}: {msg.Msg}",
                    }
                });
            }
            else
            {
                await MiraiService.SendMessageToAllGroup(default, new MessageBase[] {
                    new PlainMessage()
                    {
                        Text = $"(直播弹幕) {msg.UserName}: "
                    },
                    new ImageMessage()
                    {
                        Url = msg.MemeUrl,
                    }
                });
            }
        }
    }
}