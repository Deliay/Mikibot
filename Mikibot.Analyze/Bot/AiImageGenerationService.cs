using Microsoft.Extensions.Logging;
using Mikibot.Analyze.MiraiHttp;
using Mirai.Net.Data.Messages;
using Mirai.Net.Data.Messages.Concretes;
using Mirai.Net.Data.Messages.Receivers;
using Mirai.Net.Sessions.Http.Managers;
using Mirai.Net.Utils.Scaffolds;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using System.Threading.Tasks;
using Websocket.Client.Logging;

namespace Mikibot.Analyze.Bot
{
    public class AiImageGenerationService
    {
        private static readonly HttpClient httpClient = new()
        {
            Timeout = TimeSpan.FromMinutes(5),
        };
        private readonly ILogger<AiImageGenerationService> logger;
        private readonly IMiraiService miraiService;

        public string WebUiEndpoint { get; }

        public AiImageGenerationService(
            ILogger<AiImageGenerationService> logger,
            IMiraiService miraiService)
        {
            this.logger = logger;
            this.miraiService = miraiService;
            WebUiEndpoint = Environment.GetEnvironmentVariable("WEB_UI_ENDPOINT") ?? "http://127.0.0.1:7860/sdapi/v1/txt2img";
        }

        private readonly Channel<GroupMessageReceiver> messageQueue = Channel
        .CreateUnbounded<GroupMessageReceiver>(new UnboundedChannelOptions()
        {
            SingleWriter = true,
            AllowSynchronousContinuations = false,
        });

        private const string NegativePrompt = "lowres, bad anatomy, bad hands, text, error, missing fingers, extra digit, fewer digits, cropped, worst quality, " +
            "low quality, normal quality, jpeg artifacts, signature, watermark, username, blurry, (((look_at_viewer))),((((extra fingers)))),((look_at_viewer)), " +
            "mutated hands, ((poorly drawn hands)), paintings, sketches, (worst quality:2), (low quality:2), (normal quality:2), lowres, normal quality, ((monochrome)), " +
            "((grayscale)), skin spots, acnes, skin blemishes, age spot, glans, nipples, (((necklace))), (worst quality, low quality:1.2), watermark, username, " +
            "signature, text, multiple breasts, lowres, bad anatomy, bad hands, text, error, missing fingers, extra digit, fewer digits, cropped, worst quality, " +
            "low quality, normal quality, jpeg artifacts, signature, watermark, username, blurry, bad feet, single color, ((((ugly)))), (((duplicate))), ((morbid)), " +
            "((mutilated)), (((tranny))), (((trans))), (((trannsexual))), (hermaphrodite),((poorly drawn face)), (((mutation))), (((deformed))), ((ugly)), blurry, " +
            "((bad anatomy)), (((bad proportions))), ((extra limbs)), (((disfigured))), (bad anatomy), gross proportions, (malformed limbs), ((missing arms)), " +
            "(missing legs), (((extra arms))), (((extra legs))), mutated hands,(fused fingers), (too many fingers), (((long neck))), (bad body perspect:1.1), nsfw";

        private const string BasicPrompt = "<lora:pastelMixStylizedAnime_pastelMixLoraVersion:0.2>, " +
            "<lora:roluaStyleLora_r:0.3>,<lora:shadedface_2r16d16e:0.3>,<lora:V11ForegroundPlant_V11:0.3>, " +
            "<lora:hipoly3DModelLora_v10:0.3>," +
            "masterpiece, best quality, 1girl, solo, purple eyes, black long hair, [purple streaked hair], small breast, ";


        private static readonly Dictionary<string, List<string>> promptMap = new()
        {
            { "!来张jk弥弥", new () {
                "<lora:miki-v2+v3:0.6>, school, plant, jk, school uniform, ",
                "<lora:miki-v2+v3:0.6>, engine room, plant, jk, school uniform, ",
                "<lora:miki-v2+v3:0.6>, laboratory, jk, school uniform, ",
                "<lora:miki-v2+v3:0.6>, street, plant, jk, school uniform, ",
                "<lora:miki-v2+v3:0.6>, stairs, plant, jk, school uniform, ",
            } },
            { "!来张衬衫弥", new () {
                "<lora:miki-v2+v3:0.6>, lake, forest, skirt, plant, ",
                "<lora:miki-v2+v3:0.6>, laboratory, skirt, ",
                "<lora:miki-v2+v3:0.6>, mountain, forest, skirt, plant, ",
                "<lora:miki-v2+v3:0.6>, castle, skirt, plant, ",
                "<lora:miki-v2+v3:0.6>, street, skirt, plant, ",
                "<lora:miki-v2+v3:0.6>, dormitory, skirt, plant, ",
            } },
            { "!来张白裙弥", new () {
                "<lora:miki-v2+v3:0.6>, mountain, white dress, off-shoulder dress, bare shoulders, miki bag summer, miki v2, ",
                "<lora:miki-v2+v3:0.6>, castle, white dress, off-shoulder dress, bare shoulders, ",
                "<lora:miki-v2+v3:0.6>, dormitory, white dress, off-shoulder dress, bare shoulders, ",
                "<lora:miki-v2+v3:0.6>, street, plant, white dress, off-shoulder dress, bare shoulders, ",
                "<lora:miki-v2+v3:0.6>, street, plant, white dress, off-shoulder dress, bare shoulders, straw hat, ",
                "<lora:miki-v2+v3:0.6>, beach, sunshine, white dress, off-shoulder dress, bare shoulders, straw hat, ",
                "<lora:miki-v2+v3:0.6>, flowers meadows, sunshine, white dress, off-shoulder dress, bare shoulders, straw hat, ",
            } },
            { "!来张泳装弥", new () {
                "<lora:miki-v2+v3:0.4>, school swimsuit, poolside, ",
                "<lora:miki-v2+v3:0.4>, school swimsuit, beach, ocean, "
            } },
            { "!来张ol弥", new () {
                "<lora:miki-v2+v3:0.6>, mountain in window, office, office lady",
                "<lora:miki-v2+v3:0.6>, laboratory, office lady",
            } },
            { "!来张lo弥", new() {
                "<lora:miki-v2+v3:0.6>, gothic lolita, lolita fashion, gothic architecture, plant"
            } },
            { "!来张女仆弥", new() {
                "<lora:miki-v2+v3:0.6>, dormitory, maid, maid headdress, maid apron"
            } }
        };

        private const int CD = 120;

        private static bool HasCategory(string category)
        {
            return promptMap.ContainsKey(category);
        }

        private static readonly List<string> categories = promptMap.Keys.ToList();

        private static readonly List<string> behaviours = new()
        {
            "lap pillow", "hug", "fighting stance", "princess carry",
            "standing", "mimikaki", "sweat", "wet", "sleeping", "bathing",
        };

        private static readonly List<string> actions= new()
        {
            "eye contact","symmetrical hand pose","symmetrical docking","back-to-back","leaning forward",
            "leg hug","indian style","yokozuwari","wariza","seiza","sitting","all fours","bent over",
            "top-down bottom-up","kneeling","straddle","squatting","on stomach","lying","looking back","upside-down"
        };

        private static readonly List<string> hairStyles = new()
        {
            "wavy hair", "payot", "twin braids", "messy hair", "side ponytail", "double bun", "hair bun",
            "drill hair", "curly hair", "blunt bangs", "bangs", "wet hair", "handled hair", "holding hair", "hair spread out",
            "hair dryer", "floating hair", "adjusting hair", "wavy hair", "very short hair", "very long hair", "twintails",
            "twin braids", "tri braids", "spiked hair", "side ponytail", "side braid", "short ponytail", "short hair",
            "scrunchie", "quad braids", "ponytail", "pixie cut", "parted bangs", "messy hair", "medium hair",
            "low-braided long hair", "long hair", "lone nape hair", "huge ahoge", "hime cut", "high ponytail", "headband",
            "half updo", "hairpin", "hair slicked back", "hair over one eyes", "hair clip", "hair bun", "hair bobbles",
            "hair between eyes", "french braid", "dreadlocks", "double bun", "braided bun", "braided bangs", "braid",
            "bobcut", "blunt bangs", "big hair", "asymmetrical bangs", "antenna hair", "ahoge", "absurdly long hair",
            "bun cover", "ringlets", "comb over", "hair over eyes", "doughnut hair bun", "crown braid", "buzz cut"
        };


        private static readonly Random random = new();

        private static readonly MessageChain helpMsg = new MessageChainBuilder()
                                .Plain($"指令有5分钟的CD，可用生成如下（英文叹号）：!来张随机弥,{string.Join(',', categories)}").Build();


        private static readonly MessageChain generateMsg = new MessageChainBuilder()
                                .Plain($"生成中，请稍等").Build();

        private static MessageChain GetCdMessage()
        {
            return new MessageChainBuilder()
                .Plain($"正在生成或冷却中~ 请稍等! CD: {CD - (DateTimeOffset.Now - latestGenerateAt).TotalSeconds}秒")
                .Build();
        }

        private static T RandomOf<T>(List<T> list)
        {
            return list[random.Next(list.Count)];
        }

        private static string GetPrompt(string category)
        {
            if (!promptMap.TryGetValue(category, out var prompts))
            {
                prompts = promptMap[RandomOf(categories)]!;
            }

            var scene = RandomOf(prompts);
            if (random.Next(2) == 1)
            {
                var behaviour = RandomOf(behaviours);
                var action = RandomOf(actions);
                var hair = RandomOf(hairStyles);

                return $"{BasicPrompt}{scene}{behaviour}, {action}, {hair}, ";
            }

            return $"{BasicPrompt}{scene}";
        }

        private static DateTimeOffset latestGenerateAt = DateTimeOffset.Now.Subtract(TimeSpan.FromMinutes(5));

        private static bool IsColdingDown()
        {
            return (DateTimeOffset.Now - latestGenerateAt).TotalSeconds < CD;
        }

        private struct Info
        {
            public long seed { get; set; }
        }
        private struct Ret
        {
            public List<string> images { get; set; }
            public string info { get; set; }
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
                        if (HasCategory(plain.Text) || plain.Text == "!来张随机弥")
                        {
                            if (IsColdingDown())
                            {
                                await miraiService.SendMessageToGroup(group, token, GetCdMessage().ToArray());
                            }
                            else
                            {
                                latestGenerateAt = DateTimeOffset.Now;
                                var prompt = GetPrompt(plain.Text);
                                await miraiService.SendMessageToGroup(group, token, generateMsg.ToArray());
                                logger.LogInformation("prompt: {}", prompt);
                                var res = await httpClient.PostAsync($"{WebUiEndpoint}", JsonContent.Create(new
                                {
                                    prompt,
                                    enable_hr = true,
                                    denoising_strength = 0.6,
                                    hr_scale = 2.0,
                                    hr_upscaler = "Latent",
                                    hr_second_pass_steps = 30,
                                    cfg_scale = 10,
                                    steps = 30,
                                    sampler_index = "DPM++ 2M Karras",
                                    width = 768,
                                    height = 432,
                                    negative_prompt = NegativePrompt,
                                }), token);
                                try
                                {
                                    var body = await res.Content.ReadFromJsonAsync<Ret>(cancellationToken: token);
                                    var info = JsonSerializer.Deserialize<Info>(body.info);
                                    logger.LogInformation("生成成功，种子:{}", info.seed);
                                    if (body.images.Count != 0)
                                    {
                                        await miraiService.SendMessageToGroup(group, token, new MessageBase[]
                                        {
                                        new PlainMessage()
                                        {
                                            Text = $"生成成功，种子:{info.seed}"
                                        },
                                        new ImageMessage()
                                        {
                                            Base64 = body.images[0],
                                        },
                                        });
                                    }
                                }
                                catch (Exception ex)
                                {
                                    logger.LogError(ex, "catched after access AI!");
                                    return;
                                }
                            }
                        }
                        else if (plain.Text == "!help")
                        {
                            await miraiService.SendMessageToGroup(group, token, helpMsg.ToArray());
                        }
                    }
                }

            }
        }

        public async Task Run(CancellationToken token)
        {
            logger.LogInformation("弥图生成指令开始运行");
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
    }
}
