using Mikibot.AutoClipper.Abstract.Rquest;
using SimpleHttpServer.Pipeline;
using SimpleHttpServer.Pipeline.Middlewares;
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
        public ClipperController(ClipperService clipper)
        {
            Clipper = clipper;
        }

        private ClipperService Clipper { get; }

        private async ValueTask<StartRecordingResponse> StartRecording(StartRecording recording, CancellationToken token)
            => new () { IsStarted = await Clipper.StartRecording(recording.Bid, token) };

        public async ValueTask Route(RequestContext ctx, Func<ValueTask> next)
        {
            var router = RouterMiddleware.Route("/api/record", (route) =>
                route.Post("/", async (ctx) =>
                {
                    var body = await JsonSerializer.DeserializeAsync<StartRecording>(ctx.Http.Request.InputStream);
                    await StartRecording(body!, ctx.CancelToken);
                }));

            await router(ctx, next);
        }
    }
}
