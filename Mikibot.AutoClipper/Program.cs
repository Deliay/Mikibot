using Autofac;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mikibot.AutoClipper.Service;
using Mikibot.BuildingBlocks.Util;
using Mikibot.Crawler.Http.Bilibili;
using Mikibot.Database;
using SimpleHttpServer.Pipeline;
using SimpleHttpServer.Pipeline.Middlewares;

var appBuilder = ContainerInitializer.Create();

appBuilder.RegisterType<ClipperService>().AsSelf().SingleInstance();
appBuilder.RegisterType<ClipperController>().AsSelf().SingleInstance();
appBuilder.RegisterType<HttpServer>().AsSelf().SingleInstance();
appBuilder.RegisterType<BiliLiveCrawler>().AsSelf().SingleInstance();
appBuilder.Register<MySqlConfiguration>((_) => MySqlConfiguration.FromEnviroment());
appBuilder.RegisterType<MikibotDatabaseContext>().AsSelf().InstancePerDependency();

using var app = appBuilder.Build();
using var csc = new CancellationTokenSource();

var logger = app.Resolve<ILogger<Program>>();
logger.LogInformation("Mikibot auto clipper starting...v{}", typeof(Program).Assembly.GetName().Version);

var db = app.Resolve<MikibotDatabaseContext>();
logger.LogInformation("Initializing database connection and database structure");
await db.Database.MigrateAsync(csc.Token);
logger.LogInformation("Done");

var webServer = app.Resolve<HttpServer>();

var clipper = app.Resolve<ClipperController>()!;

webServer.AddHandlers(handle => handle
.Use(RouterMiddleware.Route("/", (route) => route.Use(clipper.Route))));

await webServer.Run(csc.Token);