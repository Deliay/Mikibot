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
            WebUiEndpoint = "http://127.0.0.1:7002/run/predict";
            WebUiFileEndpoint = "http://127.0.0.1:7002/file=";
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
                "akumaria" => (0.2f, 0.5f, 0.9f, 0.9f),
                _ => (0.2f, 0.5f, 0.9f, 1f),
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
                                var payload = GetPredictPayload(GroupVoicerMapping[group.Id], plain.Text);
                                var generateResponse = await httpClient.PostAsJsonAsync(WebUiEndpoint, payload, token);

                                var str = await generateResponse.Content.ReadAsStringAsync(token);
                                var wavTmp = $"{Path.GetTempFileName()}.wav";
                                var amrTmp = $"{Path.GetTempFileName()}.amr";
                                try
                                {
                                    var json = JsonSerializer.Deserialize<JsonDocument>(str);

                                    var data = json.RootElement.GetProperty("data");
                                    var result = data[1];
                                    var path = result.GetProperty("name");

                                    var wavData = await httpClient.GetByteArrayAsync($"{WebUiFileEndpoint}{path}", token);
                                    await File.WriteAllBytesAsync(wavTmp, wavData, token);
                                    logger.LogInformation($"wave: {wavTmp}, amr: {amrTmp}");
                                    await FFMpegArguments
                                        .FromFileInput(wavTmp)
                                        .OutputToFile(amrTmp, true, opt => opt
                                            .WithAudioSamplingRate(8000)
                                            .ForceFormat("amr"))
                                        .CancellableThrough(token)
                                        .WithLogLevel(FFMpegCore.Enums.FFMpegLogLevel.Verbose)
                                        .NotifyOnOutput(Console.WriteLine)
                                        .ProcessAsynchronously(throwOnError: true);
                                    await group.SendGroupMessageAsync(new()
                                    {
                                        new VoiceMessage()
                                        {
                                            Path = amrTmp,
                                        }
                                    });
                                }
                                catch(Exception e)
                                {
                                    logger.LogInformation(str);
                                    logger.LogError(e, "报错力");
                                }
                                finally
                                {
                                    File.Delete(wavTmp);
                                    File.Delete(amrTmp);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
