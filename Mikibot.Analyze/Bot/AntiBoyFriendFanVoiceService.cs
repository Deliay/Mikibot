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
    public struct QVoice
    {
        public Regex[] MatchRegices { get; set; }
        public MessageBase[] Messages { get; set; }

        public static QVoice Of(MessageBase[] messageBases, params Regex[] regices)
        {
            return new()
            {
                MatchRegices = regices,
                Messages = messageBases,
            };
        }
    }

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
            try
            {
                voices = new()
                {
                    QVoice.Of(LoadVoice("mxmk_is_not_your_gf.amr"), new Regex(":女朋友|:女友")),
                    QVoice.Of(LoadVoice("mxmk_laugh_hetun.amr"), new Regex(":河豚")),
                    QVoice.Of(LoadVoice("mxmk_16yrs_old.amr"), new Regex(":岁")),
                    QVoice.Of(LoadVoice("mxmk_kimo.amr"), new Regex(":恶")),
                    QVoice.Of(LoadVoice("mxmk_hso.amr"), new Regex(":色")),
                    QVoice.Of(LoadVoice("mxmk_hurt.amr"), new Regex(":伤心")),
                    QVoice.Of(LoadVoice("mxmk_baka.amr"), new Regex(":笨蛋")),
                    QVoice.Of(LoadVoice("mxmk_r18.amr"), new Regex(":男同")),
                    QVoice.Of(LoadVoice("mxmk_jj_cutted.amr"), new Regex(":阉割|:性转")),
                    QVoice.Of(LoadVoice("mxmk_awsl.amr"), new Regex(":awsl", RegexOptions.IgnoreCase)),
                    QVoice.Of(LoadVoice("mxmk_dog.amr"), new Regex(":🐕|:🐶|:狗|:dog", RegexOptions.IgnoreCase)),
                    QVoice.Of(LoadVoice("mxmk_loss.amr"), new Regex(":为什么|:为甚么")),
                    QVoice.Of(LoadVoice("mxmk_hentai.amr"), new Regex(":变态")),
                    QVoice.Of(LoadVoice("mxmk_ybb.amr"), new Regex(":病")),
                    QVoice.Of(LoadVoice("mxmk_xhs.amr"), new Regex(":小红书")),
                    QVoice.Of(LoadVoice("mxmk_shangdang.amr"), new Regex(":上当")),
                    QVoice.Of(LoadVoice("mxmk_g.amr"), new Regex(":寄")),
                    QVoice.Of(LoadVoice("mxmk_jb.amr"), new Regex(":jb", RegexOptions.IgnoreCase)),
                    QVoice.Of(LoadVoice("mxmk_lazy.amr"), new Regex(":懒")),
                    QVoice.Of(LoadVoice("mxmk_laugh.amr"), new Regex(":笑")),
                    QVoice.Of(LoadVoice("mxmk_fullfilled.amr"), new Regex(":填满")),
                    QVoice.Of(LoadVoice("mxmk_waimai.amr"), new Regex(":外卖")),
                    QVoice.Of(LoadVoice("mxmk_eatme.amr"), new Regex(":吃我")),
                    QVoice.Of(LoadVoice("mxmk_crazy.amr"), new Regex(":发病")),
                    QVoice.Of(LoadVoice("mxmk_nignth.amr"), new Regex(":晚安")),
                    QVoice.Of(LoadVoice("mxmk_morning.amr"), new Regex(":早安")),
                    QVoice.Of(LoadVoice("mxmk_huabei.amr"), new Regex(":花呗")),
                    QVoice.Of(LoadVoice("mxmk_fly.amr"), new Regex(":飞扑")),
                    QVoice.Of(LoadVoice("mxmk_ghost.amr"), new Regex(":女鬼")),
                    QVoice.Of(LoadVoice("mxmk_star_fallen.amr"), new Regex(":星降")),
                    QVoice.Of(LoadVoice("mxmk_loule.amr"), new Regex(":漏了")),
                    QVoice.Of(new MessageBase[] {
                        new PlainMessage("弥BOT按钮" +
                        ":女朋友|:女友,:河豚,:岁,:恶,:色,:伤心,:笨蛋,:男同,:阉割|:性转," +
                        ":awsl,:🐕|:🐶|:狗|:dog,:为什么|:为甚么,:变态,:病,:小红书,:上当,:寄,:jb,:懒,:笑,:填满,:外卖" +
                        ":吃我,:发病,:晚安,:早安,:花呗,:飞扑,:女鬼,:星降,:漏了"),
                    }, new Regex(":help")),
                    QVoice.Of(new MessageBase[] {
                        new PlainMessage("mxmk歌单：夏天的风、心墙、下雨天、求佛、メンヘラじゃないもん/地雷（使用::歌名点歌，如果有/，可以用/后面的简写点歌）"),
                    }, new Regex("::歌单")),
                    QVoice.Of(LoadVoice("mxmk_xtdf.amr"), new Regex("::夏天的风")),
                    QVoice.Of(LoadVoice("mxmk_xinqiang.amr"), new Regex("::心墙")),
                    QVoice.Of(LoadVoice("mxmk_xiayutian.amr"), new Regex("::下雨天")),
                    QVoice.Of(LoadVoice("mxmk_qiufo.amr"), new Regex("::求佛")),
                    QVoice.Of(LoadVoice("mxmk_menhera_ja_nai_mon.amr"), new Regex("::メンヘラじゃないもん|::地雷")),
                };
            } catch (Exception e)
            {
                logger.LogWarning(e, "语音包加载失败");
            }
        }

        private List<QVoice> voices { get; }
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
        private readonly Dictionary<QVoice, DateTimeOffset> lastVoiceSentAt = new();

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

        private bool CheckTime<T>(Dictionary<T, DateTimeOffset> set, T id, TimeSpan duration) where T : notnull
        {
            if (set.ContainsKey(id))
            {
                var time = DateTimeOffset.Now - set[id];
                Logger.LogInformation("上次发送间隔：{}s", time.TotalSeconds);
                if (time < duration)
                {
                    return false;
                }
            }
            set[id] = DateTimeOffset.Now;
            return true;
        }

        private async ValueTask SendVoiceMessage(Mirai.Net.Data.Shared.Group group, CancellationToken token, params MessageBase[] messages)
        {
            if (CheckTime(lastSentAt, group.Id, TimeSpan.FromSeconds(5)))
            {
                await MiraiService.SendMessageToGroup(group, token, messages);
            }
        }

        private async ValueTask<bool> MatchMessage(Mirai.Net.Data.Shared.Group group, PlainMessage msg, QVoice voice, CancellationToken token)
        {
            var messages = voice.Messages;
            var regices = voice.MatchRegices;
            foreach (var regex in regices)
            {
                if (!regex.IsMatch(msg.Text)) return false;
            }

            if (!CheckTime(lastVoiceSentAt, voice, TimeSpan.FromSeconds(30)))
            {
                Logger.LogInformation("[CD] 群 {} 文本 {} 匹配 {} 发送语音 {}", group.Id, msg.Text, regices, messages);
                return false;
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
                        foreach (var item in voices)
                        {
                            if (await MatchMessage(group, plain, item, token)) return;
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
