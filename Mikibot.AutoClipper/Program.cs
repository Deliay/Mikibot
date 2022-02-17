using Autofac;
using Mikibot.AutoClipper.Service;
using Mikibot.BuildingBlocks.Util;
using SimpleHttpServer.Pipeline;
using SimpleHttpServer.Pipeline.Middlewares;

var appBuilder = ContainerInitializer.Create();

appBuilder.RegisterType<ClipperService>().AsSelf().SingleInstance();
appBuilder.RegisterType<ClipperController>().AsSelf().SingleInstance();
appBuilder.RegisterType<HttpServer>().AsSelf().SingleInstance();

using var app = appBuilder.Build();
using var csc = new CancellationTokenSource();

var webServer = app.Resolve<HttpServer>();

var clipper = app.Resolve<ClipperController>()!;

webServer.AddHandlers(handle => handle
.Use(RouterMiddleware.Route("/", (route) => route.Use(clipper.Route))));

await webServer.Run(csc.Token);