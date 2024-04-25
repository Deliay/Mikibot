using Autofac;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mikibot.Database;
using Mikibot.Analyze.MiraiHttp;
using Mikibot.Analyze.Notification;
using Mikibot.Analyze.Service;
using Mikibot.BuildingBlocks.Util;
using Mikibot.Crawler.Http.Bilibili;
using Mikibot.Crawler.WebsocketCrawler.Data.Commands.KnownCommand;
using Mikibot.Analyze.Bot;
using Mikibot.Analyze.Bot.RandomImage;

var appBuilder = ContainerInitializer.Create();

appBuilder.RegisterType<BiliLiveCrawler>().AsSelf().SingleInstance();
appBuilder.RegisterType<BiliVideoCrawler>().AsSelf().SingleInstance();

appBuilder.Register((_) => MySqlConfiguration.FromEnviroment());
appBuilder.RegisterType<MikibotDatabaseContext>().AsSelf().InstancePerDependency();

appBuilder.Register((_) => MiraiBotConfig.FromEnviroment());
#if DEBUG
appBuilder.RegisterType<ConsoleMiraiService>().As<IMiraiService>().SingleInstance();
appBuilder.RegisterType<LocalOssService>().As<IOssService>().SingleInstance();
// appBuilder.RegisterType<ConsoleEmailService>().As<IEmailService>().SingleInstance();
appBuilder.RegisterType<LagrangeBotBridge>().As<IMiraiService>().SingleInstance();
#else
appBuilder.RegisterType<LagrangeBotBridge>().As<IMiraiService>().SingleInstance();
// appBuilder.RegisterType<MiraiService>().As<IMiraiService>().SingleInstance();
appBuilder.RegisterType<LocalOssService>().As<IOssService>().SingleInstance();
appBuilder.RegisterType<ConsoleEmailService>().As<IEmailService>().SingleInstance();
#endif

appBuilder.RegisterType<LiveStreamEventService>().AsSelf().SingleInstance();

appBuilder.RegisterType<LiveStatusCrawlService>().AsSelf().SingleInstance();
appBuilder.RegisterType<DailyFollowerStatisticService>().AsSelf().SingleInstance();
appBuilder.RegisterType<DanmakuCollectorService>().AsSelf().SingleInstance();
appBuilder.RegisterType<DanmakuRecordControlService>().AsSelf().SingleInstance();
appBuilder.RegisterType<DanmakuExportGuardList>().AsSelf().SingleInstance();
appBuilder.RegisterType<MikiDanmakuProxyService>().AsSelf().SingleInstance();
appBuilder.RegisterType<MikiLiveEventProxyService>().AsSelf().SingleInstance();

appBuilder.RegisterType<BiliBiliVideoLinkShareProxerService>().AsSelf().SingleInstance();
appBuilder.RegisterType<AntiBoyFriendFanVoiceService>().AsSelf().SingleInstance();
//appBuilder.RegisterType<AiImageGenerationService>().AsSelf().SingleInstance();
appBuilder.RegisterType<AiVoiceGenerationService>().AsSelf().SingleInstance();
appBuilder.RegisterType<RandomImageService>().AsSelf().SingleInstance();
appBuilder.RegisterType<OptionaSelectorService>().AsSelf().SingleInstance();
appBuilder.RegisterType<PingtiItemReplaceService>().AsSelf().SingleInstance();

var appContainer = appBuilder.Build();

using (var csc = new CancellationTokenSource())
using (var app = appContainer.BeginLifetimeScope())
{
    var token = csc.Token;

    var logger = app.Resolve<ILogger<Program>>();

    logger.LogInformation("Mikibot starting...v{}", typeof(Program).Assembly.GetName().Version);

    var db = app.Resolve<MikibotDatabaseContext>();
    logger.LogInformation("Initializing database connection and database structure");
    await db.Database.MigrateAsync(token);
    logger.LogInformation("Done");

    var mirai = app.Resolve<IMiraiService>();
    logger.LogInformation("Intiializing mirai service...");
    await mirai.Run();
    logger.LogInformation("Done");


    var statusCrawler = app.Resolve<LiveStatusCrawlService>();
    var followerStat = app.Resolve<DailyFollowerStatisticService>();
    // var eventService = app.Resolve<LiveStreamEventService>();

    // var danmakuClip = app.Resolve<DanmakuRecordControlService>();
    // var danmakuCrawler = app.Resolve<DanmakuCollectorService>();
    // var danmakuExportGuard = app.Resolve<DanmakuExportGuardList>();

    var aiVoice = app.Resolve<AiVoiceGenerationService>();
    //var aiImage = app.Resolve<AiImageGenerationService>();
    var bffAnti = app.Resolve<AntiBoyFriendFanVoiceService>();
    var mxmkDanmakuProxy = app.Resolve<MikiDanmakuProxyService>();
    var mxmkLiveEventProxy = app.Resolve<MikiLiveEventProxyService>();
    var biliParser = app.Resolve<BiliBiliVideoLinkShareProxerService>();
    var randomImage = app.Resolve<RandomImageService>();
    var optionaSelector = app.Resolve<OptionaSelectorService>();
    var pingti = app.Resolve<PingtiItemReplaceService>();

    // eventService.CmdHandler.Register(danmakuCrawler);
    // eventService.CmdHandler.Register(mxmkLiveEventProxy);

    // eventService.CmdHandler.Subscribe<DanmuMsg>(danmakuClip.HandleDanmu);
    // eventService.CmdHandler.Subscribe<DanmuMsg>(danmakuExportGuard.HandleDanmaku);
    // eventService.CmdHandler.Subscribe<DanmuMsg>(mxmkDanmakuProxy.HandleDanmaku);

    logger.LogInformation("Starting schedule module...");
    await Task.WhenAll(
    [
        statusCrawler.Run(token),
        followerStat.Run(token),
        // eventService.Run(token),
        bffAnti.Run(token),
        biliParser.Run(token),
        randomImage.Run(token),
        //aiImage.Run(token),
        aiVoice.Run(token),   
        optionaSelector.Run(token),
        // pingti.Run(token),
    ]);
}
