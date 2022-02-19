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
                if (int.TryParse(ctx.Http.Request.QueryString["Bid"], out var bid))
                {
                    await ctx.Http.Response.Ok(await StartLoopRecording(bid, ctx.CancelToken));
                }
                else
                {
                    await ctx.Http.Response.NotFound();
                }

            })
            .Delete("/loop-record", async (ctx) =>
            {
                if (int.TryParse(ctx.Http.Request.QueryString["Bid"], out var bid))
                {
                    await StopLoopRecording(bid, ctx.CancelToken);
                    await ctx.Http.Response.Ok(ctx.CancelToken);
                }
                else
                {
                    await ctx.Http.Response.NotFound();
                }
            })
            .Post("/danmaku-record", async (ctx) =>
            {
                if (int.TryParse(ctx.Http.Request.QueryString["Bid"], out var bid))
                {
                    await ctx.Http.Response.Ok(await StartDanmakuRecording(bid, ctx.CancelToken));
                }
                else
                {
                    await ctx.Http.Response.NotFound();
                }
            })
            .Delete("/danmaku-record", async (ctx) =>
            {
                if (int.TryParse(ctx.Http.Request.QueryString["Bid"], out var bid))
                {
                    await StopDanmakuRecording(bid, ctx.CancelToken);
                    await ctx.Http.Response.Ok(ctx.CancelToken);
                }
                else
                {
                    await ctx.Http.Response.NotFound();
                }
            }));
        }

        private ClipperService Clipper { get; }
        public ILogger<ClipperController> Logger { get; }

        private async ValueTask<StartRecordingResponse> StartDanmakuRecording(int bid, CancellationToken token)
            => new() { IsStarted = await Clipper.StartDanmakuRecording(bid, token) };

        private async ValueTask StopDanmakuRecording(int bid, CancellationToken token)
            => await Clipper.StopDanmakuRecording(bid, token);

        private async ValueTask<StartRecordingResponse> StartLoopRecording(int bid, CancellationToken token)
            => new () { IsStarted = await Clipper.StartLoopRecording(bid, token) };

        private async ValueTask StopLoopRecording(int bid, CancellationToken token)
            => await Clipper.CancelLoopRecording(bid, token);

    }
}
