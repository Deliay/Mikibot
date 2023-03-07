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
using System.Text.RegularExpressions;
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

        private const string BasicPrompt = "<lora:pastelMixStylizedAnime_pastelMixLoraVersion:0.3>, " +
            "<lora:roluaStyleLora_r:0.2>,<lora:shadedface_2r16d16e:0.2>,<lora:V11ForegroundPlant_V11:0.3>, " +
            "masterpiece, best quality, 1girl, solo, ";


        private static readonly Dictionary<string, List<string>> promptMap = new()
        {
            { "jk", new () {
                "<lora:miki-v2+v3:0.6>, school, plant, jk, school uniform, ",
                "<lora:miki-v2+v3:0.6>, engine room, plant, jk, school uniform, ",
                "<lora:miki-v2+v3:0.6>, laboratory, jk, school uniform, ",
                "<lora:miki-v2+v3:0.6>, street, plant, jk, school uniform, ",
                "<lora:miki-v2+v3:0.6>, stairs, plant, jk, school uniform, ",
            } },
            { "萝莉", new () {
                "(loli), <lora:miki-v2+v3:0.6>, school, plant, loli, ",
                "(loli), <lora:miki-v2+v3:0.6>, laboratory, js, loli, ",
                "(loli), <lora:miki-v2+v3:0.6>, street, plant, js, loli, ",
                "(loli), <lora:miki-v2+v3:0.6>, stairs, plant, js, loli, ",
                "(loli), <lora:miki-v2+v3:0.6>, school, plant, mesugaki, loli, ",
                "(loli), <lora:miki-v2+v3:0.6>, laboratory, js, mesugaki, loli, ",
                "(loli), <lora:miki-v2+v3:0.6>, street, plant, js, mesugaki, loli, ",
                "(loli), <lora:miki-v2+v3:0.6>, stairs, plant, js, mesugaki, loli, ",
            } },
            { "Q版", new () {
                "(loli), <lora:miki-v2+v3:0.6>, school, plant, (chibi), loli, ",
                "(loli), <lora:miki-v2+v3:0.6>, laboratory, js, (chibi), loli, ",
                "(loli), <lora:miki-v2+v3:0.6>, street, plant, js, (chibi), loli, ",
                "(loli), <lora:miki-v2+v3:0.6>, stairs, plant, js, (chibi), loli, ",
                "(loli), <lora:miki-v2+v3:0.6>, school, plant, mesugaki, (chibi), loli, ",
                "(loli), <lora:miki-v2+v3:0.6>, laboratory, js, mesugaki, (chibi), loli, ",
                "(loli), <lora:miki-v2+v3:0.6>, street, plant, js, mesugaki, (chibi), loli, ",
                "(loli), <lora:miki-v2+v3:0.6>, stairs, plant, js, mesugaki, (chibi), loli, ",
            } },
            { "衬衫", new () {
                "<lora:miki-v2+v3:0.6>, lake, forest, skirt, plant, ",
                "<lora:miki-v2+v3:0.6>, laboratory, skirt, ",
                "<lora:miki-v2+v3:0.6>, mountain, forest, skirt, plant, ",
                "<lora:miki-v2+v3:0.6>, castle, skirt, plant, ",
                "<lora:miki-v2+v3:0.6>, street, skirt, plant, ",
                "<lora:miki-v2+v3:0.6>, dormitory, skirt, plant, ",
            } },
            { "白裙", new () {
                "<lora:miki-v2+v3:0.6>, mountain, white dress, off-shoulder dress, bare shoulders, miki bag summer, miki v2, ",
                "<lora:miki-v2+v3:0.6>, castle, white dress, off-shoulder dress, bare shoulders, ",
                "<lora:miki-v2+v3:0.6>, dormitory, white dress, off-shoulder dress, bare shoulders, ",
                "<lora:miki-v2+v3:0.6>, street, plant, white dress, off-shoulder dress, bare shoulders, ",
                "<lora:miki-v2+v3:0.6>, street, plant, white dress, off-shoulder dress, bare shoulders, straw hat, ",
                "<lora:miki-v2+v3:0.6>, beach, sunshine, white dress, off-shoulder dress, bare shoulders, straw hat, ",
                "<lora:miki-v2+v3:0.6>, flowers meadows, sunshine, white dress, off-shoulder dress, bare shoulders, straw hat, ",
            } },
            { "泳装", new () {
                "<lora:miki-v2+v3:0.5>, school swimsuit, poolside, ",
                "<lora:miki-v2+v3:0.5>, school swimsuit, beach, ocean, ",
                "<lora:miki-v2+v3:0.5>, one-piece swimsuit, poolside, ",
                "<lora:miki-v2+v3:0.5>, one-piece swimsuit, beach, ocean, ",
                "<lora:miki-v2+v3:0.5>, side-tie bikini bottom, beach, ocean, ",
            } },
            { "ol", new () {
                "<lora:miki-v2+v3:0.6>, mountain in window, office lady",
                "<lora:miki-v2+v3:0.6>, laboratory, office lady, ",
                "<lora:miki-v2+v3:0.6>, dormitory, office lady, ",
                "<lora:hipoly3DModelLora_v10:0.2>, <lora:miki-v2+v3:0.6>, dormitory, office lady, ",
                "<lora:hipoly3DModelLora_v10:0.2>, <lora:miki-v2+v3:0.6>, laboratory, office lady, ",
                "<lora:hipoly3DModelLora_v10:0.2>, <lora:miki-v2+v3:0.6>, mountain in window, office lady, ",
            } },
            { "lo", new() {
                "<lora:miki-v2+v3:0.5>, gothic lolita, lolita fashion, gothic architecture, plant, ",
                "(loli), <lora:miki-v2+v3:0.5>, gothic lolita, lolita fashion, gothic architecture, plant, chibi, loli, ",
                "(loli), <lora:miki-v2+v3:0.5>, gothic lolita, lolita fashion, gothic architecture, plant, mesugaki, loli, ",
                "(loli), <lora:miki-v2+v3:0.5>, gothic lolita, lolita fashion, gothic architecture, plant, mesugaki, loli, ",
            } },
            { "女仆", new() {
                "<lora:miki-v2+v3:0.5>, dormitory, maid, maid headdress, maid apron, ",
                "<lora:miki-v2+v3:0.5>, street, maid, maid headdress, maid apron, ",
                "<lora:miki-v2+v3:0.5>, castle, maid, maid headdress, maid apron, ",
                "<lora:miki-v2+v3:0.5>, mountain, maid, maid headdress, maid apron, ",
                "<lora:miki-v2+v3:0.5>, forest, maid, maid headdress, maid apron, ",
            } },
            { "旗袍", new() {
                "<lora:miki-v2+v3:0.6>, chinese, dormitory, (red china dress), ",
                "<lora:miki-v2+v3:0.6>, chinese, chinese street, (red china dress), ",
                "<lora:miki-v2+v3:0.6>, chinese, chinese mountain, (red china dress), ",
                "<lora:miki-v2+v3:0.6>, chinese, chinese forest, lake, (red china dress), ",
                "<lora:miki-v2+v3:0.6>, chinese, dormitory, (red chinese clothes), ",
                "<lora:miki-v2+v3:0.6>, chinese, chinese street, (red chinese clothes), ",
                "<lora:miki-v2+v3:0.6>, chinese, chinese mountain, (red chinese clothes), ",
                "<lora:miki-v2+v3:0.6>, chinese, chinese forest, lake, (red chinese clothes), ",
            } },
            { "机甲", new() {
                "<lora:miki-v2+v3:0.5>, cyberpunk, kabuto, japanese armor, japanese clothes, holding tantou, (machine:1.2),(translucent:1.2),false limb, prosthetic weapon, tentacles, ",
                "<lora:miki-v2+v3:0.5>, cyberpunk, kabuto, japanese armor, japanese clothes, (machine:1.2),(translucent:1.2),false limb, prosthetic weapon, tentacles, ",
                "<lora:miki-v2+v3:0.5>, kabuto, japanese armor, holding tantou, (machine:1.2),(translucent:1.2),false limb, prosthetic weapon, ",
                "<lora:miki-v2+v3:0.5>, kabuto, japanese armor, japanese clothes, (machine:1.2),(translucent:1.2),false limb, prosthetic weapon, ",
                "<lora:miki-v2+v3:0.5>, kabuto, japanese armor, (machine:1.2),(translucent:1.2),false limb, prosthetic weapon, ",
            } },
        };

        private const int CD = 30;

        private static bool HasCategory(string category)
        {
            return promptMap.ContainsKey(category);
        }

        private static readonly List<string> categories = promptMap.Keys.ToList();

        private static readonly List<string> behaviours = new()
        {
            "lap pillow", "hug", "fighting stance", "princess carry",
            "standing", "mimikaki", "sweat", "wet", "sleeping", "bathing", "shaded face",
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
            "drill hair", "curly hair", "blunt bangs", "bangs", "handled hair", "holding hair", "hair spread out",
            "hair dryer", "floating hair", "adjusting hair", "wavy hair", "very short hair", "very long hair", "twintails",
            "twin braids", "tri braids", "spiked hair", "side ponytail", "side braid", "short ponytail", "short hair",
            "scrunchie", "quad braids", "ponytail", "pixie cut", "parted bangs", "messy hair", "medium hair",
            "low-braided long hair", "long hair", "lone nape hair", "huge ahoge", "hime cut", "high ponytail", "headband",
            "half updo", "hairpin", "hair slicked back", "hair over one eyes", "hair clip", "hair bun", "hair bobbles",
            "hair between eyes", "french braid", "dreadlocks", "double bun", "braided bun", "braided bangs", "braid",
            "bobcut", "blunt bangs", "big hair", "asymmetrical bangs", "antenna hair", "ahoge", "absurdly long hair",
            "bun cover", "ringlets", "comb over", "doughnut hair bun", "crown braid", "buzz cut",
            "(side braid)"
        };

        private static readonly List<string> emotions = new()
        {
            ":>","rectangular mouth",":<>",":c","o3o","x3",":o",":>",":>",":<",":<",":p",">:(",">:)",":d","angry",
            "blush","bored","depressed","despair","disdain", "nose blush","sleepy",
            "sobbing","turn pale","torogao","tongue","teeth","tears","surprised","smile","skin fang","singing sang",
            "sigh","shaded face","serious","screaming","scared","sad","round teeth","raised eyebrow","pout","pain",
            "orgasm","open mouth","nervous","naughty face","light smile","licking","gloom depressed","fucked silly",
            "frown","fang","expressionless","embarrassed","drunk","drooling","disgust","confused","clenched teeth",
            "annoyed","ahegao","looking at viewer","open mouth","clenched teeth","lips","eyeball","eyelid pull",
            "food on face","wink","dark persona","shy"
        };

        private static readonly List<string> rolePalys = new()
        {
            "yuri","milf","kemonomimi mode","minigirl","furry","magical girl","vampire","devil","monster","angel",
            "elf","fairy","mermaid","nun","ninja","doll","cheerleader","waitress","maid","miko","witch"
        };

        private static readonly List<string> scenes = new()
        {
            "cityscape", "scenery", "loong, dragon background, loong background", "rose", "petal", "coconut tree", 
            "pine tree", "maple tree", "cypress", "fruit tree", "treehouse", "twig", "tree stump", "tree shade", 
            "rainy days", "night", "full moon", "on a desert", "in a meadow", "in hawaii", "in winter", "in autumn", 
            "in summer", "in spring", "futon", "sofa", "train", "locker room", "hallway", "telephone pole", "underwater", 
            "ruins", "magic circle", "pentagram background", "christmas tree", "cherry tree", "building", "bathtub", 
            "starry sky", "sea", 
            "forest,outline,fountain,shop,outdoors,east asian architecture,greco-roman architecture,restaurant,shanty town,slum", 
            "cyberpunk, city, kowloon, rain", "starry sky,clusters of stars,starry sky,glinting stars", "floating sakura", 
            "violet background", "glinting stars", "floating white feathers", "aurora", "snowflakes", "snowfield", "cafe", 
            "grassland", "blue sky with clouds", "electricity", "rain", "blur background", "farm", "red moon", "battlefield", 
            "alpine", "coffee house", "lawn", "on bed", "fireworks", "isekai cityscape", "flying butterfly", "beach background", 
            "dungeon background", "airport background", "cityscape", "space background", "beige background", "simple pattern background", 
            "shooting star", "cherry blossoms", "colorful startrails", "white background", "silhouette", "gradient background", 
            "sunburst background", "starry sky,clusters of stars", "snow", "sunlight on the desk ,8K ,high definition, 8K", 
            "building,rain,neon lights,cumulonimbus,moon"
        };

        private static readonly Random random = new();
        private static MessageChain GetGenerateMsg(string extra)
        {
            return new MessageChainBuilder()
                               .Plain($"生成中，请稍等\n")
                               .Plain(extra).Build();
        }

        private static MessageChain GetCdMessage()
        {
            return new MessageChainBuilder()
                .Plain($"正在生成或冷却中~ 请稍等! \n冷却时间: {CD - (DateTimeOffset.Now - latestGenerateAt).TotalSeconds}秒。\n\nTip:直到冷却时间转好为止不会再进行提示~")
                .Build();
        }

        private static T RandomOf<T>(List<T> list)
        {
            return list[random.Next(list.Count)];
        }

        private const string DefaultLora = "miki-v2+v3";
        private static (string, string) GetPrompt(string style, string character)
        {
            if (!promptMap.TryGetValue(style, out var prompts))
            {
                prompts = promptMap[RandomOf(categories)]!;
            }

            var lora = characterLore[character];
            var main = RandomOf(prompts).Replace(DefaultLora, lora);
            var hair = RandomOf(hairStyles);
            var emo = RandomOf(emotions);
            var fullbody = random.Next(100) > 50 ? "full body" : "";
            var extra = "";

            var prefix = characterPrefix.GetValueOrDefault(character) ?? "";

            if (random.Next(2) == 1)
            {
                var scene = RandomOf(scenes);
                var behaviour = RandomOf(behaviours);
                var action = RandomOf(actions);
                var rp = RandomOf(rolePalys);

                extra = $"{behaviour}, {action}, {rp}, {scene}, ";
            }

            return ($"{BasicPrompt}{prefix}{main}({emo}), {hair}, {extra}, {fullbody}", $"生成词：{main}{fullbody}\n发型:{hair}\n表情:{emo}\n附加词 {extra}");
        }

        private static DateTimeOffset latestGenerateAt = DateTimeOffset.Now.Subtract(TimeSpan.FromMinutes(5));

        private static bool IsColdingDown()
        {
            return (DateTimeOffset.Now - latestGenerateAt).TotalSeconds < CD;
        }
        private bool isCdHintShown = false;

        private struct Info
        {
            public long seed { get; set; }
        }
        private struct Ret
        {
            public List<string> images { get; set; }
            public string info { get; set; }
        }

        private static readonly Dictionary<string, HashSet<string>> characterLimit = new()
        {
            { "弥", new() { "139528984" } },
            { "真", new() { "139528984" } },
            { "悠", new() { "139528984" } },
            { "侑", new() { "139528984" } },
            { "炉", new() { "139528984" } },
        };

        private static readonly Dictionary<string, string> characterLore = new()
        {
            { "弥", "miki-v2+v3" },
            { "真", "mahiru-v2" },
            { "悠", "YuaVirtuareal_v01" },
            { "侑", "KiyuuVirtuareal_v20" },
            { "炉", "kaoru" },
        };

        private static readonly Dictionary<string, string> characterPrefix = new()
        {
            { "弥", "purple eyes, black hair, [purple streaked hair], small breast, " },
            { "真", "yellow eyes, red hair, small breast, demon girl, demon tail, demon wings, " },
            { "悠", "(light blue eyes), black hair ribbon, silver hair, blue streaked hair, " },
            { "侑", "(white pink hair), (blue streaked hair), (cat_ear_headphone), <lora:Kiyuu_:0.3>, " },
            { "炉", "yellow eyes, pink to cyan gradient short hair, gradient hair, cyan bleach, ahoge, beret, hat, <lora:kaoru:0.2>, " },
        };

        private static readonly MessageChain helpMsg = new MessageChainBuilder()
                                .Plain($"指令有2分钟的CD，使用'!来张[风格][人物]'生成（需要英文括号）\n\n例子：!来张随机弥\n可用人物:{string.Join(',', characterLore.Keys)}\n可用风格\n：随机,{string.Join(',', categories)}").Build();

        private static (string, string) parseCommand(string raw)
        {
            var match = Regex.Matches(raw, "!来张(.*?)(.)$").FirstOrDefault();
            if (match is null) {
                return ("", "");
            }
            return (match.Result("$1"), match.Result("$2"));
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
                        if (plain.Text == "!help")
                        {
                            await miraiService.SendMessageToGroup(group, token, helpMsg.ToArray());
                        }
                        var (style, character) = parseCommand(plain.Text);
                        if (character == "" )
                        {
                            continue;
                        }
                        if (!characterLimit.TryGetValue(character, out var groups)) {
                            continue;
                        }
                        if (!groups.Contains(group.Id)) {
                            continue;
                        }
                        if (HasCategory(style) || style == "随机")
                        {
                            if (IsColdingDown())
                            {
                                if (!isCdHintShown)
                                {
                                    await miraiService.SendMessageToGroup(group, token, GetCdMessage().ToArray());
                                    isCdHintShown = true;
                                }
                            }
                            else
                            {
                                latestGenerateAt = DateTimeOffset.Now;
                                isCdHintShown = false;
                                var (prompt, extra) = GetPrompt(style, character);
                                await miraiService.SendMessageToGroup(group, token, GetGenerateMsg(extra).ToArray());
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
                                latestGenerateAt = DateTimeOffset.Now;
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
