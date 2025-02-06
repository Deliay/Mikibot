using Autofac;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Mikibot.Database;
using Mikibot.Analyze.MiraiHttp;
using Mikibot.Analyze.Notification;
using Mikibot.Analyze.Service;
using Mikibot.BuildingBlocks.Util;
using Mikibot.Crawler.Http.Bilibili;
using Mikibot.Analyze.Bot;
using Mikibot.Analyze.Bot.FoodDice;
using Mikibot.Analyze.Generic;
using Mikibot.Analyze.Service.Ai;

var appBuilder = ContainerInitializer.Create();

appBuilder.RegisterType<HttpClient>().AsSelf().SingleInstance();
appBuilder.RegisterType<BiliLiveCrawler>().AsSelf().SingleInstance();
appBuilder.RegisterType<BiliVideoCrawler>().AsSelf().SingleInstance();

appBuilder.Register((_) => MySqlConfiguration.FromEnviroment());
appBuilder.RegisterType<MikibotDatabaseContext>().AsSelf().InstancePerDependency();

appBuilder.Register((_) => MiraiBotConfig.FromEnviroment());
// #if DEBUG
// appBuilder.RegisterType<ConsoleMiraiService>().As<IMiraiService>().SingleInstance();
// appBuilder.RegisterType<LocalOssService>().As<IOssService>().SingleInstance();
// // appBuilder.RegisterType<ConsoleEmailService>().As<IEmailService>().SingleInstance();
// // appBuilder.RegisterType<LagrangeBotBridge>().As<IMiraiService>().SingleInstance();
// #else
// appBuilder.RegisterType<LagrangeBotBridge>().As<IMiraiService>().SingleInstance();
// appBuilder.RegisterType<MiraiService>().As<IMiraiService>().SingleInstance();
appBuilder.RegisterType<SatoriBotBridge>().As<IMiraiService>().SingleInstance();
// appBuilder.RegisterType<LocalOssService>().As<IOssService>().SingleInstance();
// appBuilder.RegisterType<ConsoleEmailService>().As<IEmailService>().SingleInstance();
// #endif

// appBuilder.RegisterType<LiveStreamEventService>().AsSelf().SingleInstance();

appBuilder.RegisterType<LiveStatusCrawlService>().AsSelf().SingleInstance();
appBuilder.RegisterType<DailyFollowerStatisticService>().AsSelf().SingleInstance();
// appBuilder.RegisterType<DanmakuCollectorService>().AsSelf().SingleInstance();
// appBuilder.RegisterType<DanmakuRecordControlService>().AsSelf().SingleInstance();
// appBuilder.RegisterType<DanmakuExportGuardList>().AsSelf().SingleInstance();
// appBuilder.RegisterType<MikiDanmakuProxyService>().AsSelf().SingleInstance();
// appBuilder.RegisterType<MikiLiveEventProxyService>().AsSelf().SingleInstance();

appBuilder.RegisterType<BiliBiliVideoLinkShareProxyService>().AsSelf().SingleInstance();
// appBuilder.RegisterType<AntiBoyFriendFanVoiceService>().AsSelf().SingleInstance();
//appBuilder.RegisterType<AiImageGenerationService>().AsSelf().SingleInstance();
// appBuilder.RegisterType<AiVoiceGenerationService>().AsSelf().SingleInstance();
// appBuilder.RegisterType<RandomImageService>().AsSelf().SingleInstance();
appBuilder.RegisterType<OptionSelectorService>().AsSelf().SingleInstance();
appBuilder.RegisterType<PingtiItemReplaceService>().AsSelf().SingleInstance();
appBuilder.RegisterType<SubscribeService>().AsSelf().SingleInstance();
appBuilder.RegisterType<FoodDiceService>().AsSelf().SingleInstance();
appBuilder.RegisterType<PermissionService>().AsSelf().SingleInstance();
appBuilder.RegisterType<LlmChatbot>().AsSelf().SingleInstance();
appBuilder.RegisterType<ChatHistoryService>().AsSelf().SingleInstance();
appBuilder.RegisterType<ChatbotSwitchService>().AsSelf().SingleInstance();

var chatbotVendor = Environment.GetEnvironmentVariable("CHATBOT_VENDOR") ?? "ollama";

if (chatbotVendor == "deepseek") 
    appBuilder.RegisterType<DeepSeekAiChatService>().As<IBotChatService>().SingleInstance();
else
    appBuilder.RegisterType<OllamaChatService>().As<IBotChatService>().SingleInstance();


var appContainer = appBuilder.Build();

using var csc = new CancellationTokenSource();
await using var app = appContainer.BeginLifetimeScope();

var token = csc.Token;

var logger = app.Resolve<ILogger<Program>>();

logger.LogInformation("Mikibot starting...v{}", typeof(Program).Assembly.GetName().Version);

var db = app.Resolve<MikibotDatabaseContext>();
logger.LogInformation("Initializing database connection and database structure");
await db.Database.MigrateAsync(token);
logger.LogInformation("Done");

var mirai = app.Resolve<IMiraiService>();
logger.LogInformation("Initializing mirai service...");
await mirai.Run();

var statusCrawler = app.Resolve<LiveStatusCrawlService>();
var followerStat = app.Resolve<DailyFollowerStatisticService>();
var biliParser = app.Resolve<BiliBiliVideoLinkShareProxyService>();
var optionaSelector = app.Resolve<OptionSelectorService>();
var pingti = app.Resolve<PingtiItemReplaceService>();
var subscribe = app.Resolve<SubscribeService>();
var foodDice = app.Resolve<FoodDiceService>();
var chatBot = app.Resolve<LlmChatbot>();
var chatHistory = app.Resolve<ChatHistoryService>();

logger.LogInformation("Starting schedule module...");
await Task.WhenAll(
[
    statusCrawler.Run(token),
    followerStat.Run(token),
    biliParser.Run(token),
    optionaSelector.Run(token),
    pingti.Run(token),
    subscribe.Run(token),
    foodDice.Run(token),
    chatBot.Run(token),
    chatHistory.Run(token)
]);
