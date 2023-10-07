using FFMpegCore;
using FFMpegCore.Pipes;
using Microsoft.Extensions.Logging;
using Mikibot.Analyze.MiraiHttp;
using Mirai.Net.Data.Messages;
using Mirai.Net.Data.Messages.Concretes;
using Mirai.Net.Data.Messages.Receivers;
using Mirai.Net.Sessions.Http.Managers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Mikibot.Analyze.Bot
{
    public class AiVoiceGenerationService
    {
        private static readonly HttpClient httpClient = new()
        {
            Timeout = TimeSpan.FromMinutes(5),
        };


        private readonly IMiraiService miraiService;
        public string WebUiEndpoint { get; }
        public string WebUiFileEndpoint { get; }

        private readonly ILogger<AiVoiceGenerationService> logger;

        public AiVoiceGenerationService(IMiraiService miraiService, ILogger<AiVoiceGenerationService> logger)
        {
            this.miraiService = miraiService;
            WebUiEndpoint = Environment.GetEnvironmentVariable("WEB_UI_ENDPOINT") ?? "http://127.0.0.1:7860/run/predict";
            WebUiFileEndpoint = Environment.GetEnvironmentVariable("WEB_UI_ENDPOINT") ?? "http://127.0.0.1:7860/file=";
            this.logger = logger;
        }

        private readonly Channel<GroupMessageReceiver> messageQueue = Channel
        .CreateUnbounded<GroupMessageReceiver>(new UnboundedChannelOptions()
        {
            SingleWriter = true,
            AllowSynchronousContinuations = false,
        });

        public async Task Run(CancellationToken token)
        {
            logger.LogInformation("AI语音bot开始运行");
            miraiService.SubscribeMessage((msg) => { _ = messageQueue.Writer.WriteAsync(msg); }, token);
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await messageQueue.Reader.WaitToReadAsync(token);
                    await Dequeue(token);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "弥图生成失败, error=");
                }
            }
        }

        private static readonly Dictionary<string, string> GroupVoicerMapping = new()
        {
            { "972488523", "akumaria" },
            { "314503649", "miki" },
            { "139528984", "miki" },
        };

        private static (float, float, float, float) GetVoiceArguments(string voice)
        {
            return voice switch
            {
                "akumaria" => (0.2f, 0.6f, 0.8f, 0.8f),
                _ => (0.2f, 0.6f, 0.8f, 1f),
            };
        }

        private class GradioPredictResult
        {

        }

        private object GetPredictPayload(string voice, string text)
        {
            var (sdp, ns, nsw, ls) = GetVoiceArguments(voice);
            return new
            {
                data = new ArrayList() { text, voice, sdp, ns, nsw, ls },
                event_data = (object)null!,
                fn_index = 0,
            };
        }

        private async ValueTask Dequeue(CancellationToken token)
        {

            await foreach (var msg in messageQueue.Reader.ReadAllAsync(token))
            {
                var group = msg.Sender.Group;

                if (!GroupVoicerMapping.ContainsKey(group.Id))
                {
                    continue;
                }

                foreach (var rawMsg in msg.MessageChain)
                {
                    if (rawMsg is PlainMessage plain)
                    {
                        if (plain.Text.Trim().StartsWith("#"))
                        {
                            if (msg.Sender.Id == "644676751" || msg.Sender.Id == "1441685502")
                            {
                                try
                                {
                                    var payload = GetPredictPayload(GroupVoicerMapping[group.Id], plain.Text);
                                    var generateResponse = await httpClient.PostAsJsonAsync(WebUiEndpoint, payload, token);

                                    using var stream = await generateResponse.Content.ReadAsStreamAsync(token);
                                    var json = await JsonSerializer.DeserializeAsync<JsonDocument>(stream, cancellationToken: token);

                                    var data = json.RootElement.GetProperty("data");
                                    var result = data[1];
                                    var path = result.GetProperty("name");

                                    using var aacStream = new MemoryStream();
                                    var wavStream = await httpClient.GetStreamAsync($"{WebUiFileEndpoint}{path}", token);
                                    await FFMpegArguments
                                        .FromPipeInput(new StreamPipeSource(wavStream), opt => opt.ForceFormat("wav"))
                                        .OutputToPipe(new StreamPipeSink(aacStream), opt => opt.ForceFormat("aac"))
                                        .CancellableThrough(token)
                                        .ProcessAsynchronously(throwOnError: true);

                                    await group.SendGroupMessageAsync(new()
                                    {
                                        new VoiceMessage()
                                        {
                                            Base64 = Convert.ToBase64String(aacStream.GetBuffer())
                                        }
                                    });
                                }
                                catch(Exception e)
                                {
                                    logger.LogError(e, "报错力");
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
