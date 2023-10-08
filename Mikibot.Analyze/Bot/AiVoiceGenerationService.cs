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
            WebUiEndpoint = Environment.GetEnvironmentVariable("BERT_VITS_PREDICT_ENDPOINT") ?? "http://127.0.0.1:7002/run/predict";
            WebUiFileEndpoint = Environment.GetEnvironmentVariable("BERT_VITS_GET_FILE_ENDPOINT") ?? "http://127.0.0.1:7002/file=";
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
            { "1097589845", "ayelet" },
        };

        private static (float, float, float, float) GetVoiceArguments(string voice)
        {
            return voice switch
            {
                "ayelet" => (0.1f, 0.5f, 0.9f, 0.9f),
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

        private static readonly SemaphoreSlim _dailyRestrict = new(1);
        private const string CONST_VoiceDailyFile = "voice_records.json";

        private async ValueTask<Dictionary<string, Dictionary<string, int>>> Read(CancellationToken token)
        {
            if (!File.Exists(CONST_VoiceDailyFile))
            {
                await File.WriteAllTextAsync(CONST_VoiceDailyFile, "{}", token);
            }
            using var readStream = File.OpenRead(CONST_VoiceDailyFile);
            return await JsonSerializer.DeserializeAsync<Dictionary<string, Dictionary<string, int>>>(readStream, cancellationToken: token) ?? new();
        }

        private async ValueTask Write(Dictionary<string, Dictionary<string, int>> records, CancellationToken token)
        {
            using var writeStream = File.OpenWrite(CONST_VoiceDailyFile);

            await JsonSerializer.SerializeAsync(writeStream, records);
        }

        private async Task<bool> CanSendVoice(string senderId, CancellationToken token) {
            await _dailyRestrict.WaitAsync(token);
            try
            {
                var today = DateTime.Now.ToString("yyyy-MM-dd");
                var records = await Read(token);
                if (!records.ContainsKey(today))
                {
                    records.Add(today, new());
                }
                var todayRecords = records[today];
                if (!todayRecords.ContainsKey(senderId))
                {
                    todayRecords.Add(senderId, 1);
                }
                
                var result = (todayRecords[senderId] += 1) <= 3;

                await Write(records, token);

                return result;
            }
            finally
            {
                _dailyRestrict.Release();
            }
        }

        private static readonly HashSet<string> voiceWhiteList = new()
        {
            "644676751",
            "1441685502",
        };

        private async ValueTask GenerateVoiceToFile(string file, string voicer, string content, CancellationToken token)
        {
            var payload = GetPredictPayload(voicer, content);
            var generateResponse = await httpClient.PostAsJsonAsync(WebUiEndpoint, payload, token);

            var str = await generateResponse.Content.ReadAsStringAsync(token);

            var json = JsonSerializer.Deserialize<JsonDocument>(str);

            var data = json.RootElement.GetProperty("data");
            var result = data[1];
            var path = result.GetProperty("name");

            var wavData = await httpClient.GetByteArrayAsync($"{WebUiFileEndpoint}{path}", token);
            await File.WriteAllBytesAsync(file, wavData, token);
        }

        private async ValueTask FfmpegConvert(string wav, string amr, CancellationToken token)
        {
            await FFMpegArguments
                .FromFileInput(wav)
                .OutputToFile(amr, true, opt => opt
                    .WithAudioSamplingRate(8000)
                    .ForceFormat("amr"))
                .CancellableThrough(token)
                .ProcessAsynchronously(throwOnError: true);
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
                    // 只处理纯文本消息
                    if (rawMsg is not PlainMessage plain)
                    {
                        continue;
                    }
                    // 超长不处理，消息必须以#开头
                    if (plain.Text.Length > 500 || !plain.Text.Trim().StartsWith("#"))
                    {
                        continue;
                    }
                    var senderId = msg.Sender.Id;
                    // 不在白名单，且每日超过3次，则不允许再发
                    if (!voiceWhiteList.Contains(senderId) && !await CanSendVoice(senderId, token))
                    {
                        continue;
                    }
                    // 群没有指定语音角色
                    if (!GroupVoicerMapping.ContainsKey(group.Id))
                    {
                        continue;
                    }
                    // 生成语音

                    var wavTmp = $"{Path.GetTempFileName()}.wav";
                    var amrTmp = $"{Path.GetTempFileName()}.amr";
                    try
                    {
                        await GenerateVoiceToFile(wavTmp, GroupVoicerMapping[group.Id], plain.Text, token);

                        await FfmpegConvert(wavTmp, amrTmp, token);

                        logger.LogInformation($"sending AI voice from {senderId}, content: {plain.Text}, wave: {wavTmp}, amr: {amrTmp}");

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
