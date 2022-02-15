using Autofac;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mikibot.Analyze.Database;
using Mikibot.Analyze.MiraiHttp;
using Mikibot.Analyze.Notification;
using Mikibot.Analyze.Service;
using Mikibot.Analyze.Util;
using Mikibot.Crawler.Http.Bilibili;
using Mikibot.Crawler.WebsocketCrawler;
using Mikibot.Crawler.WebsocketCrawler.Client;
using Mikibot.Crawler.WebsocketCrawler.Data.Commands.KnownCommand;

var appBuilder = ContainerInitializer.Create();

appBuilder.RegisterType<BiliLiveCrawler>().AsSelf().SingleInstance();
appBuilder.Register<MySqlConfiguration>((_) => MySqlConfiguration.FromEnviroment());
appBuilder.RegisterType<MikibotDatabaseContext>().AsSelf().InstancePerDependency();

appBuilder.Register<MiraiBotConfig>((_) => MiraiBotConfig.FromEnviroment());
#if DEBUG
appBuilder.RegisterType<ConsoleMiraiService>().As<IMiraiService>().SingleInstance();
#else
appBuilder.RegisterType<MiraiService>().As<IMiraiService>().SingleInstance();
#endif

appBuilder.RegisterType<LiveStreamEventService>().AsSelf().SingleInstance();

appBuilder.RegisterType<LiveStatusCrawlService>().AsSelf().SingleInstance();
appBuilder.RegisterType<DailyFollowerStatisticService>().AsSelf().SingleInstance();
appBuilder.RegisterType<DanmakuCollectorService>().AsSelf().SingleInstance();

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
    var eventService = app.Resolve<LiveStreamEventService>();

    var danmakuCrawler = app.Resolve<DanmakuCollectorService>();

    eventService.CmdHandler.Register(danmakuCrawler);

    logger.LogInformation("Starting schedule module...");
    await Task.WhenAll(statusCrawler.Run(token), followerStat.Run(token), eventService.Run(token));
}
