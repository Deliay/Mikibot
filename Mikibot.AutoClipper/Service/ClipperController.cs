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
        public Func<RequestContext, Func<ValueTask>, ValueTask> Route { get; }
        public ClipperController(ClipperService clipper, ILogger<ClipperController> logger)
        {
            Clipper = clipper;
            Logger = logger;
            Route = RouterMiddleware.Route("/api", (route) => route
            .Post("/loop-record", async (ctx) =>
            {
                var body = await JsonSerializer.DeserializeAsync<StartRecording>(ctx.Http.Request.InputStream);

                await ctx.Http.Response.Ok(await StartLoopRecording(body!, ctx.CancelToken));
            })
            .Delete("/loop-record", async (ctx) =>
            {
                var body = await JsonSerializer.DeserializeAsync<StartRecording>(ctx.Http.Request.InputStream);
                await StopLoopRecording(body!, ctx.CancelToken);
                await ctx.Http.Response.Ok();
            })
            .Post("/danmaku-record", async (ctx) =>
            {
                var body = await JsonSerializer.DeserializeAsync<StartRecording>(ctx.Http.Request.InputStream);

                await ctx.Http.Response.Ok(await StartDanmakuRecording(body!, ctx.CancelToken));
            })
            .Delete("/danmaku-record", async (ctx) =>
            {
                var body = await JsonSerializer.DeserializeAsync<StartRecording>(ctx.Http.Request.InputStream);
                await StopDanmakuRecording(body!, ctx.CancelToken);
                await ctx.Http.Response.Ok();
            }));
        }

        private ClipperService Clipper { get; }
        public ILogger<ClipperController> Logger { get; }

        private async ValueTask<StartRecordingResponse> StartDanmakuRecording(StartRecording recording, CancellationToken token)
            => new() { IsStarted = await Clipper.StartDanmakuRecording(recording.Bid, token) };

        private async ValueTask StopDanmakuRecording(StartRecording recording, CancellationToken token)
            => await Clipper.StopDanmakuRecording(recording.Bid, token);

        private async ValueTask<StartRecordingResponse> StartLoopRecording(StartRecording recording, CancellationToken token)
            => new () { IsStarted = await Clipper.StartLoopRecording(recording.Bid, token) };

        private async ValueTask StopLoopRecording(StartRecording recording, CancellationToken token)
            => await Clipper.CancelLoopRecording(recording.Bid, token);

    }
}
