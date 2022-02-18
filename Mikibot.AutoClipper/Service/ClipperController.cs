using Microsoft.Extensions.Logging;
using Mikibot.AutoClipper.Abstract.Rquest;
using SimpleHttpServer.Pipeline;
using SimpleHttpServer.Pipeline.Middlewares;
using SimpleHttpServer.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Mikibot.AutoClipper.Service
{
    public class ClipperController
    {
        public ClipperController(ClipperService clipper, ILogger<ClipperController> logger)
        {
            Clipper = clipper;
            Logger = logger;
        }

        private ClipperService Clipper { get; }
        public ILogger<ClipperController> Logger { get; }

        private async ValueTask<StartRecordingResponse> StartLoopRecording(StartRecording recording, CancellationToken token)
            => new () { IsStarted = await Clipper.StartLoopRecording(recording.Bid, token) };

        private async ValueTask StopLoopRecording(StartRecording recording, CancellationToken token)
            => await Clipper.CancelLoopRecording(recording.Bid, token);

        public async ValueTask Route(RequestContext ctx, Func<ValueTask> next)
        {
            var router = RouterMiddleware.Route("/api/loop-record", (route) => route
            .Post("", async (ctx) =>
            {
                var body = await JsonSerializer.DeserializeAsync<StartRecording>(ctx.Http.Request.InputStream);

                await ctx.Http.Response.Ok(await StartLoopRecording(body!, ctx.CancelToken));
            })
            .Delete("", async (ctx) =>
            {
                var body = await JsonSerializer.DeserializeAsync<StartRecording>(ctx.Http.Request.InputStream);
                await StopLoopRecording(body!, ctx.CancelToken);
                await ctx.Http.Response.Ok();
            }));

            await router(ctx, next);
        }
    }
}
