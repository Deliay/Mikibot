using Microsoft.Extensions.Logging;
using Mikibot.Analyze.MiraiHttp;
using Mirai.Net.Data.Messages;
using Mirai.Net.Data.Messages.Concretes;
using Mirai.Net.Data.Messages.Receivers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Mikibot.Analyze.Bot
{
    public class AntiBoyFriendFanVoiceService
    {
        public AntiBoyFriendFanVoiceService(
            IMiraiService miraiService,
            ILogger<AntiBoyFriendFanVoiceService> logger)
        {
            MiraiService = miraiService;
            Logger = logger;
            VoiceBaseDir = Environment.GetEnvironmentVariable("MIKI_VOICE_DIR") ?? Path.GetTempPath();

            Logger.LogInformation("弥弥语音包位置：{}", VoiceBaseDir);

            notYourGrilFriend = LoadVoice("mxmk_is_not_your_gf.wav");
        }

        private IMiraiService MiraiService { get; }
        private ILogger<AntiBoyFriendFanVoiceService> Logger { get; }
        public string VoiceBaseDir { get; }

        private readonly Channel<GroupMessageReceiver> messageQueue = Channel
        .CreateUnbounded<GroupMessageReceiver>(new UnboundedChannelOptions()
            {
                SingleWriter = true,
                AllowSynchronousContinuations = false,
            });

        private void FilterMessage(GroupMessageReceiver message)
        {
            _ = messageQueue.Writer.WriteAsync(message);
        }

        private Dictionary<string, DateTimeOffset> lastSentAt = new();

        private MessageBase[] LoadVoice(string filename)
        {
            var path = Path.Combine(VoiceBaseDir, filename);
            return new MessageBase[]
            {
                new VoiceMessage()
                {
                    Base64 = Convert.ToBase64String(File.ReadAllBytes(path)),
                }
            };
        }
        private readonly MessageBase[] notYourGrilFriend;

        private async ValueTask Dequeue(CancellationToken token)
        {
            await foreach (var msg in this.messageQueue.Reader.ReadAllAsync(token))
            {
                var gId = msg.Sender.Group.Id;

                foreach (var rawMsg in msg.MessageChain)
                {
                    if (rawMsg is PlainMessage plain)
                    {
                        Logger.LogInformation("[QQ群] {}({}) 发言：{}", msg.Sender.Name, msg.Sender.Id, plain.Text);
                        if (Regex.IsMatch(plain.Text, "弥|mxmk|毛线毛裤") && Regex.IsMatch(plain.Text, "女朋友|女友|结婚|男友|恋爱|老婆|二胎|三胎|孩子名字|想我|好喜欢你|🤤|😍|🥰|我的弥"))
                        {
                            Logger.LogInformation("检测到男友粉 {}({})", msg.Sender.Name, msg.Sender.Id);

                            if (lastSentAt.ContainsKey(gId))
                            {
                                if (DateTimeOffset.Now - lastSentAt[gId] < TimeSpan.FromMinutes(2))
                                {
                                    continue;
                                }
                            }
                            await MiraiService.SendMessageToGroup(msg.Sender.Group, token, notYourGrilFriend);
                            lastSentAt.Add(gId, DateTimeOffset.Now);
                            continue;
                        }
                    }
                }

            }
        }

        public async Task Run(CancellationToken token)
        {
            Logger.LogInformation("Anti男友粉机器人启动中");
            MiraiService.SubscribeMessage(FilterMessage, token);
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await messageQueue.Reader.WaitToReadAsync(token);
                    Logger.LogInformation("开始消费男友粉发言...");
                    await Dequeue(token);
                }
                catch (Exception ex)
                {
                    Logger.LogError("男友粉anit出错！", ex);
                }
            }
        }
    }
}
