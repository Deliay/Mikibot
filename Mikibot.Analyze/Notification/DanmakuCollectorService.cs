using Autofac;
using Microsoft.Extensions.Logging;
using Mikibot.Database;
using Mikibot.Database.Model;
using Mikibot.Analyze.Service;
using Mikibot.Crawler.Http.Bilibili;
using Mikibot.Crawler.WebsocketCrawler.Client;
using Mikibot.Crawler.WebsocketCrawler.Data.Commands;
using Mikibot.Crawler.WebsocketCrawler.Data.Commands.KnownCommand;
using Mikibot.Crawler.WebsocketCrawler.Data.Commands.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mikibot.Analyze.Notification
{
    public static class CollectorCommandSubscriberRegisterHelper
    {
        public static void Register(this CommandSubscriber subscriber, DanmakuCollectorService danmakuCollector)
        {
            subscriber.Subscribe<DanmuMsg>(danmakuCollector.HandleDanmu);
            subscriber.Subscribe<GuardBuy>(danmakuCollector.HandleBuyGuard);
            subscriber.Subscribe<SendGift>(danmakuCollector.HandleGift);
            subscriber.Subscribe<ComboSend>(danmakuCollector.HandleGiftCombo);
            subscriber.Subscribe<EntryEffect>(danmakuCollector.HandleGuardEnter);
            subscriber.Subscribe<InteractWord>(danmakuCollector.HandleInteractive);
            subscriber.Subscribe<SuperChatMessage>(danmakuCollector.HandleSuperChat);
        }
    }

    public class DanmakuCollectorService
    {
        private readonly MikibotDatabaseContext db = new(MySqlConfiguration.FromEnviroment());

        public ILogger<DanmakuCollectorService> Logger { get; }

        public DanmakuCollectorService(ILogger<DanmakuCollectorService> logger)
        {
            Logger = logger;
        }


        public async Task HandleDanmu(DanmuMsg msg)
        {
            Logger.LogInformation(
                "[弹幕] (Lv.{} {}){}: {} {}",
                msg.FansLevel,
                msg.FansTag,
                msg.UserName,
                msg.Msg,
                msg.MemeUrl);
            await db.LiveDanmakus.AddAsync(new LiveDanmaku()
            {
                Bid = LiveStreamEventService.mxmk,
                UserId = msg.UserId,
                UserName = msg.UserName,
                FansLevel = msg.FansLevel,
                FansTag = msg.FansTag,
                FansTagUserId = msg.FansTagUserId,
                FansTagUserName = msg.FansTagUserName,
                SentAt = msg.SentAt,
                Msg = msg.Msg,
            });
            await db.SaveChangesAsync();
        }

        public async Task HandleBuyGuard(GuardBuy msg)
        {
            Logger.LogInformation("[上舰] ({}){}: (种类:{})￥{}", msg.Uid, msg.UserName, msg.Price, msg.GuardLevel);
            await db.LiveBuyGuardLogs.AddAsync(new LiveBuyGuardLog()
            {
                Bid = LiveStreamEventService.mxmk,
                Uid = msg.Uid,
                UserName = msg.UserName,
                BoughtAt = msg.StartedAt,
                GiftName = msg.GiftName,
                GuardLevel = $"{msg.GuardLevel}",
                Price = msg.Price,
            });
            await db.SaveChangesAsync();
        }

        public async Task HandleGift(SendGift msg)
        {
            Logger.LogInformation("[礼物] ({}){}: ({})￥{}, 付费:{}", msg.SenderUid, msg.SenderName, msg.GiftName, msg.DiscountPrice, msg.CoinType);
            await db.AddAsync(new LiveGift()
            {
                Uid = msg.SenderUid,
                Action = msg.Action,
                Bid = LiveStreamEventService.mxmk,
                CoinType = msg.CoinType,
                ComboId = msg.ComboId,
                DiscountPrice = msg.DiscountPrice,
                GiftName = msg.GiftName,
                SentAt = msg.SentAt,
                UserName = msg.SenderName,
            });
            await db.SaveChangesAsync();
        }

        public async Task HandleGiftCombo(ComboSend msg)
        {
            Logger.LogInformation("[礼物] 连击 ({}){}: {},连击{},总价{}", msg.SenderUid, msg.SenderName, msg.GiftName, msg.ComboNum, msg.TotalCoin);
            await db.AddAsync(new LiveGiftCombo()
            {
                Action = msg.Action,
                Bid = LiveStreamEventService.mxmk,
                ComboId = msg.ComboId,
                ComboNum = msg.ComboNum,
                GiftName = msg.GiftName,
                TotalCoin = msg.TotalCoin,
                Uid = msg.SenderUid,
                UserName = msg.SenderName,
            });
            await db.SaveChangesAsync();
        }

        public async Task HandleGuardEnter(EntryEffect msg)
        {
            Logger.LogInformation("[舰长] {}进入: {}", msg.UserId, msg.CopyWriting);
            await db.AddAsync(new LiveGuardEnterLog()
            {
                Bid = LiveStreamEventService.mxmk,
                CopyWriting = msg.CopyWriting,
                EnteredAt = msg.EnteredAt,
                GuardLevel = msg.GuardLevel,
                UserId = msg.UserId,
            });
            await db.SaveChangesAsync();
        }

        public async Task HandleInteractive(InteractWord msg)
        {
            Logger.LogInformation("[进入] (Lv.{} {}, {}){} 进入了直播间", msg.FansMedal.MedalLevel, msg.FansMedal.MedalName, msg.UserId, msg.UserName);
            await db.AddAsync(new LiveUserInteractiveLog()
            {
                Bid = LiveStreamEventService.mxmk,
                UserId = msg.UserId,
                FansTagUserId = msg.FansMedal.FansTagUserId,
                GuardLevel = msg.FansMedal.GuardLevel,
                InteractedAt = msg.InteractAt,
                MedalLevel = msg.FansMedal.MedalLevel,
                MedalName = msg.FansMedal.MedalName,
                UserName = msg.UserName,
            });
            await db.SaveChangesAsync();
        }

        public async Task HandleSuperChat(SuperChatMessage msg)
        {
            Logger.LogInformation("[SC] (Lv.{} {}){}: ￥{} {}", msg.MedalInfo.Level, msg.MedalInfo.Name, msg.User.UserName, msg.Price, msg.Message);
            await db.AddAsync(new LiveSuperChat()
            {
                Bid = LiveStreamEventService.mxmk,
                Price = msg.Price,
                Message = msg.Message,
                MedalGuardLevel = msg.MedalInfo.GuardLevel,
                MedalLevel = msg.MedalInfo.Level,
                MedalName = msg.MedalInfo.Name,
                MedalUserId = msg.MedalInfo.MedalUserId,
                UserName = msg.User.UserName,
                SentAt = msg.SendAt,
                Uid = msg.UserId,
            });
            await db.SaveChangesAsync();
        }
    }
}
