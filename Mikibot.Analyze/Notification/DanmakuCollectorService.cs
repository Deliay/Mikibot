using Autofac;
using Microsoft.Extensions.Logging;
using Mikibot.Analyze.Database;
using Mikibot.Analyze.Database.Model;
using Mikibot.Crawler.Http.Bilibili;
using Mikibot.Crawler.WebsocketCrawler.Client;
using Mikibot.Crawler.WebsocketCrawler.Data.Commands;
using Mikibot.Crawler.WebsocketCrawler.Data.Commands.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mikibot.Analyze.Notification
{
    public class DanmakuCollectorService
    {
        private const int mxmk = 21672023;
        private readonly MikibotDatabaseContext db = new(MySqlConfiguration.FromEnviroment());

        public DanmakuCollectorService(ILifetimeScope scope)
        {
            Scope = scope;
            Logger = scope.Resolve<ILogger<DanmakuCollectorService>>();
            Crawler = scope.Resolve<BiliLiveCrawler>();
            CmdHandler = new CommandSubscriber();

            CmdHandler.Subscribe<DanmuMsg>(HandleDanmu);
            CmdHandler.Subscribe<GuardBuy>(HandleBuyGuard);
            CmdHandler.Subscribe<SendGift>(HandleGift);
            CmdHandler.Subscribe<ComboSend>(HandleGiftCombo);
            CmdHandler.Subscribe<EntryEffect>(HandleGuardEnter);
            CmdHandler.Subscribe<InteractWord>(HandleInteractive);
        }

        public ILifetimeScope Scope { get; }
        public ILogger<DanmakuCollectorService> Logger { get; }
        public BiliLiveCrawler Crawler { get; }
        public CommandSubscriber CmdHandler { get; }

        private async Task HandleDanmu(DanmuMsg msg)
        {
            Logger.LogInformation("[弹幕] (Lv.{} {}){}: {}", msg.FansLevel, msg.FansTag, msg.UserName, msg.Msg);
            await db.LiveDanmakus.AddAsync(new LiveDanmaku()
            {
                Bid = mxmk,
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

        private async Task HandleBuyGuard(GuardBuy msg)
        {
            Logger.LogInformation("[上舰] ({}){}: (种类:{})￥{}", msg.Uid, msg.UserName, msg.Price, msg.GuardLevel);
            await db.LiveBuyGuardLogs.AddAsync(new LiveBuyGuardLog()
            {
                Bid = mxmk,
                Uid = msg.Uid,
                UserName = msg.UserName,
                BoughtAt = msg.StartedAt,
                GiftName = msg.GiftName,
                GuardLevel = msg.GuardLevel,
                Price = msg.Price,
            });
            await db.SaveChangesAsync();
        }

        private async Task HandleGift(SendGift msg)
        {
            Logger.LogInformation("[礼物] ({}){}: ({})￥{}, 付费:{}", msg.SenderUid, msg.SenderName, msg.GiftName, msg.DiscountPrice, msg.CoinType);
            await db.AddAsync(new LiveGift()
            {
                Uid = msg.SenderUid,
                Action = msg.Action,
                Bid = mxmk,
                CoinType = msg.CoinType,
                ComboId = msg.ComboId,
                DiscountPrice = msg.DiscountPrice,
                GiftName = msg.GiftName,
                SentAt = msg.SentAt,
                UserName = msg.SenderName,
            });
            await db.SaveChangesAsync();
        }

        private async Task HandleGiftCombo(ComboSend msg)
        {
            Logger.LogInformation("[礼物] 连击 ({}){}: {},连击{},总价{}", msg.SenderUid, msg.SenderName, msg.GiftName, msg.ComboNum, msg.TotalCoin);
            await db.AddAsync(new LiveGiftCombo()
            {
                Action = msg.Action,
                Bid = mxmk,
                ComboId = msg.ComboId,
                ComboNum = msg.ComboNum,
                GiftName = msg.GiftName,
                TotalCoin = msg.TotalCoin,
                Uid = msg.SenderUid,
                UserName = msg.SenderName,
            });
            await db.SaveChangesAsync();
        }

        private async Task HandleGuardEnter(EntryEffect msg)
        {
            Logger.LogInformation("[舰长] {}进入: {}", msg.UserId, msg.CopyWriting);
            await db.AddAsync(new LiveGuardEnterLog()
            {
                Bid = mxmk,
                CopyWriting = msg.CopyWriting,
                EnteredAt = msg.EnteredAt,
                GuardLevel = msg.GuardLevel,
                UserId = msg.UserId,
            });
            await db.SaveChangesAsync();
        }

        private async Task HandleInteractive(InteractWord msg)
        {
            Logger.LogInformation("[进入] (Lv.{} {}, {}){} 进入了直播间", msg.FansMedal.MedalLevel, msg.FansMedal.MedalName, msg.UserId, msg.UserName);
            await db.AddAsync(new LiveUserInteractiveLog()
            {
                Bid = mxmk,
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

        private async Task ConnectAsync(CancellationToken token)
        {
            using var wsClient = new WebsocketClient();

            Logger.LogInformation("准备连接到房间: {}....", mxmk);
            var realRoomId = await Crawler.GetRealRoomId(mxmk, token);
            var spectatorEndpoint = await Crawler.GetLiveToken(realRoomId, token);
            var spectatorHost = spectatorEndpoint.Hosts[0];

            Logger.LogInformation("准备连接到服务器: ws://{}:{}....", spectatorHost.Host, spectatorHost.Port);
            await wsClient.ConnectAsync(spectatorHost.Host, spectatorHost.WsPort, realRoomId, spectatorEndpoint.Token, cancellationToken: token);

            Logger.LogInformation("准备连接到房间: ws://{}:{}....连接成功", spectatorHost.Host, spectatorHost.Port);
            await foreach (var @event in wsClient.Events(token))
            {
                await CmdHandler.Handle(@event);
            }
        }

        private int failedRetry = 0;

        public async Task Run(CancellationToken token)
        {
            while (!token.IsCancellationRequested && failedRetry++ < 5)
            {
                try { await ConnectAsync(token); }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "在抓弹幕的时候发生异常");
                }
            }
        }
    }
}
