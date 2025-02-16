﻿using FFMpegCore;
using Microsoft.Extensions.Logging;
using Mikibot.Analyze.Generic;
using Mikibot.Analyze.MiraiHttp;
using Mirai.Net.Data.Messages.Concretes;
using Mirai.Net.Data.Messages.Receivers;
using Mirai.Net.Sessions.Http.Managers;
using System.Collections;
using System.Net.Http.Json;
using System.Text.Json;

namespace Mikibot.Analyze.Bot;

public class AiVoiceGenerationService(IMiraiService miraiService, ILogger<AiVoiceGenerationService> logger)
    : MiraiGroupMessageProcessor<AiVoiceGenerationService>(miraiService, logger)
{
    private static readonly HttpClient httpClient = new()
    {
        Timeout = TimeSpan.FromMinutes(5),
    };

    public string WebUiEndpoint { get; } = Environment.GetEnvironmentVariable("BERT_VITS_PREDICT_ENDPOINT") ?? "http://127.0.0.1:7002/run/predict";
    public string WebUiFileEndpoint { get; } = Environment.GetEnvironmentVariable("BERT_VITS_GET_FILE_ENDPOINT") ?? "http://127.0.0.1:7002/file=";

    private static readonly Dictionary<string, string> GroupVoicerMapping = new()
    {
        { "650042418", "akumaria" },
        { "314503649", "miki" },
        { "139528984", "miki" },
        { "1097589845", "ayelet" },
        { "45587035", "miki" },
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

    private static object GetPredictPayload(string voice, string text)
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

    private static async ValueTask<Dictionary<string, Dictionary<string, int>>> Read(CancellationToken token)
    {
        if (!File.Exists(CONST_VoiceDailyFile))
        {
            await File.WriteAllTextAsync(CONST_VoiceDailyFile, "{}", token);
        }
        using var readStream = File.OpenRead(CONST_VoiceDailyFile);
        return await JsonSerializer.DeserializeAsync<Dictionary<string, Dictionary<string, int>>>(readStream, cancellationToken: token) ?? new();
    }

    private static async ValueTask Write(Dictionary<string, Dictionary<string, int>> records, CancellationToken token)
    {
        using var writeStream = File.OpenWrite(CONST_VoiceDailyFile);

        await JsonSerializer.SerializeAsync(writeStream, records, cancellationToken: token);
    }

    private static async Task<bool> CanSendVoice(string senderId, CancellationToken token)
    {
        await _dailyRestrict.WaitAsync(token);
        try
        {
            var today = DateTime.Now.ToString("yyyy-MM-dd");
            var records = await Read(token);
            if (!records.TryGetValue(today, out Dictionary<string, int>? value))
            {
                value = [];
                records.Add(today, value);
            }
            var todayRecords = value;
            todayRecords.TryAdd(senderId, 1);
            var result = (todayRecords[senderId] += 1) <= 10;

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

    private static async ValueTask FfmpegConvert(string wav, string amr, CancellationToken token)
    {
        await FFMpegArguments
            .FromFileInput(wav)
            .OutputToFile(amr, true, opt => opt
                .WithAudioSamplingRate(8000)
                .ForceFormat("amr"))
            .CancellableThrough(token)
            .ProcessAsynchronously(throwOnError: true);
    }


    protected override async ValueTask Process(GroupMessageReceiver msg, CancellationToken token = default)
    {
        var group = msg.Sender.Group;

        if (!GroupVoicerMapping.TryGetValue(group.Id, out string? value))
        {
            return;
        }

        foreach (var rawMsg in msg.MessageChain)
        {
            var senderId = msg.Sender.Id;
            // 只处理纯文本消息
            if (rawMsg is not PlainMessage plain)
            {
                continue;
            }
            // 超长不处理，消息必须以#开头
            if (plain.Text.Length > 500 || !plain.Text.Trim().StartsWith('#'))
            {
                continue;
            }
            // 不在白名单，且每日超过3次，则不允许再发
            if (!voiceWhiteList.Contains(senderId) && !await CanSendVoice(senderId, token))
            {
                Logger.LogInformation("{} - {} 超过限制", group.Id, senderId);
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
                await GenerateVoiceToFile(wavTmp, value, plain.Text, token);

                await FfmpegConvert(wavTmp, amrTmp, token);

                Logger.LogInformation($"sending AI voice from {senderId}, content: {plain.Text}, wave: {wavTmp}, amr: {amrTmp}");

                await group.SendGroupMessageAsync(
                [
                    new VoiceMessage()
                    {
                        Base64 = Convert.ToBase64String(await File.ReadAllBytesAsync(amrTmp, token)),
                    }
                ]);
            }
            catch (Exception e)
            {
                Logger.LogError(e, "报错力");
            }
            finally
            {
                File.Delete(wavTmp);
                File.Delete(amrTmp);
            }
        }
    }
}