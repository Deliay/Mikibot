using Mikibot.Crawler.WebsocketCrawler.Data.Commands.KnownCommand;
using Mikibot.Crawler.WebsocketCrawler.Data.Commands.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mikibot.Analyze.Notification
{
    public static class SpeedStatCommandSubscriberRegisterHelper
    {
        public static void Register(this CommandSubscriber subscriber, DanmakuSpeedStatisticService speedStatisticService)
        {

        }
    }

    /// <summary>
    /// 按照弹幕流速自动切片
    /// </summary>
    public class DanmakuSpeedStatisticService
    {
        public async Task HandleDanmu(DanmuMsg msg)
        {

        }
    }
}
