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
        private readonly static Regex[] notYourGrilFriendRegex = new Regex[]
        {
            new Regex("弥|mxmk|毛线毛裤"),
            new Regex("女朋友|女友|结婚|男友|恋爱|老婆|二胎|三胎|孩子名字|想我|好喜欢你|🤤|😍|🥰|我的弥|爱了|爱你|嘿嘿嘿|超私|超我|超死|脚香|闻脚|舔脚")
        };
        private readonly MessageBase[] notYourGrilFriend;

        private readonly static Regex[] laughHetunRegex = new Regex[]
        {
            new Regex("mihiru|mhr|hsmk|和mhr|和真真"),
            new Regex("do|复辟|结婚|二胎|三胎|四胎|联动|连体|磨|不灭")
        };
        private readonly MessageBase[] laughHetun;

        private readonly static Regex[] always16YearsOldRegex = new Regex[]
        {
            new Regex("弥|mxmk|毛线毛裤"),
            new Regex("年龄|姨|弥哥哥|姐"),
        };
        private readonly MessageBase[] always16YearsOld;

        private readonly static Regex[] kimoRegex = new Regex[]
        {
            new Regex("露点|18g|带g|太gn了|呃呃|恶心|屎尿屁|snp|酸奶片|排放环节|摄入环节|银趴|淫趴"),
        };
        private readonly MessageBase[] kimo;

        public AntiBoyFriendFanVoiceService(
            IMiraiService miraiService,
            ILogger<AntiBoyFriendFanVoiceService> logger)
        {
            MiraiService = miraiService;
            Logger = logger;
            VoiceBaseDir = Environment.GetEnvironmentVariable("MIKI_VOICE_DIR") ?? Path.GetTempPath();

            Logger.LogInformation("弥弥语音包位置：{}", VoiceBaseDir);

            notYourGrilFriend = LoadVoice("mxmk_is_not_your_gf.amr");
            always16YearsOld = LoadVoice("mxmk_16yrs_old.amr");
            laughHetun = LoadVoice("mxmk_laugh_hetun.amr");
            kimo = LoadVoice("mxmk_kimo.amr");
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

        private readonly Dictionary<string, DateTimeOffset> lastSentAt = new();

        private MessageBase[] LoadVoice(string filename)
        {
            var path = Path.Combine(VoiceBaseDir, filename);
            Logger.LogInformation("加载语音：{}", path);
            return new MessageBase[]
            {
                new VoiceMessage()
                {
                    Base64 = Convert.ToBase64String(File.ReadAllBytes(path)),
                }
            };
        }

        private async ValueTask SendVoiceMessage(Mirai.Net.Data.Shared.Group group, CancellationToken token, params MessageBase[] messages)
        {
            if (lastSentAt.ContainsKey(group.Id))
            {
                var time = DateTimeOffset.Now - lastSentAt[group.Id];
                Logger.LogInformation("上次发送间隔：{}s", time.TotalSeconds);
                if (time < TimeSpan.FromMinutes(2))
                {
                    return;
                }
            }
            await MiraiService.SendMessageToGroup(group, token, messages);
            lastSentAt.Add(group.Id, DateTimeOffset.Now);
            return;
        }

        private async ValueTask<bool> MatchMessage(Mirai.Net.Data.Shared.Group group, PlainMessage msg, MessageBase[] messages, Regex[] regices, CancellationToken token)
        {
            foreach (var regex in regices)
            {
                if (!regex.IsMatch(msg.Text)) return false;
            }

            Logger.LogInformation("群 {} 文本 {} 匹配 {} 发送语音 {}", group.Id, msg.Text, regices, messages);
            await SendVoiceMessage(group, token, messages);
            return true;
        }

        private async ValueTask Dequeue(CancellationToken token)
        {
            await foreach (var msg in this.messageQueue.Reader.ReadAllAsync(token))
            {
                var group = msg.Sender.Group;

                foreach (var rawMsg in msg.MessageChain)
                {
                    if (rawMsg is PlainMessage plain)
                    {
                        Logger.LogInformation("[QQ群] {}({}) 发言：{}", msg.Sender.Name, msg.Sender.Id, plain.Text);
                        if (!await MatchMessage(group, plain, notYourGrilFriend, notYourGrilFriendRegex, token))
                        if (!await MatchMessage(group, plain, always16YearsOld, always16YearsOldRegex, token))
                        if (!await MatchMessage(group, plain, laughHetun, laughHetunRegex, token))
                        if (!await MatchMessage(group, plain, kimo, kimoRegex, token))
                            { }
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
