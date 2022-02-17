using Microsoft.Extensions.Logging;
using SimpleHttpServer.Host;
using SimpleHttpServer.Pipeline;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mikibot.AutoClipper.Service
{
    public class HttpServer
    {
        private ILogger<HttpServer> Logger { get; }
        private SimpleHost Host { get; }

        public HttpServer(ILogger<HttpServer> logger)
        {
            Logger = logger;
            Host = new SimpleHostBuilder()
                .ConfigureServer((server) => server.ListenLocalPort(19999))
                .Build();
        }
        public void AddHandlers(Action<IRequestPipeline<RequestContext>> builder)
        {
            Host.AddHandlers(builder);
        }

        public async Task Run(CancellationToken token)
        {
            Logger.LogInformation("Server will listening at port {}", 19999);
            await Host.Run(token);
        }
    }
}
