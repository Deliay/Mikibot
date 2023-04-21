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
using Mikibot.BuildingBlocks.Util;
using NPOI.Util;
using Mikibot.StableDiffusion.WebUi.Api.Models;
using NPOI.SS.Formula.Functions;
using Mikibot.Analyze.Service;
using QWeatherAPI.Result.WeatherDailyForecast;
using QWeatherAPI.Result.GeoAPI.CityLookup;

namespace Mikibot.Analyze.Bot
{
    public partial class AiImageGenerationService
    {
        private static readonly HttpClient httpClient = new()
        {
            Timeout = TimeSpan.FromMinutes(5),
        };
        private readonly ILogger<AiImageGenerationService> logger;
        private readonly IMiraiService miraiService;
        private readonly QWeatherService weatherService;
        public string WebUiEndpoint { get; }

        public AiImageGenerationService(
            ILogger<AiImageGenerationService> logger,
            IMiraiService miraiService)
        {
            AiImageColorAdjustUtility.Initialize();
            this.logger = logger;
            this.miraiService = miraiService;
            WebUiEndpoint = Environment.GetEnvironmentVariable("WEB_UI_ENDPOINT") ?? "http://127.0.0.1:7860/sdapi/v1/txt2img";
            weatherService = new QWeatherService();
        }

        private readonly Channel<GroupMessageReceiver> messageQueue = Channel
        .CreateUnbounded<GroupMessageReceiver>(new UnboundedChannelOptions()
        {
            SingleWriter = true,
            AllowSynchronousContinuations = false,
        });

        private const string NegativePromptAnything = "lowres, bad anatomy, bad hands, text, error, missing fingers, extra digit, fewer digits, cropped, worst quality, " +
            "low quality, normal quality, jpeg artifacts, signature, watermark, username, blurry, (((look_at_viewer))),((((extra fingers)))),((look_at_viewer)), " +
            "mutated hands, ((poorly drawn hands)), paintings, sketches, (worst quality:2), (low quality:2), (normal quality:2), lowres, normal quality, ((monochrome)), " +
            "((grayscale)), skin spots, acnes, skin blemishes, age spot, glans, nipples, (((necklace))), (worst quality, low quality:1.2), watermark, username, " +
            "signature, text, multiple breasts, lowres, bad anatomy, bad hands, text, error, missing fingers, extra digit, fewer digits, cropped, worst quality, " +
            "low quality, normal quality, jpeg artifacts, signature, watermark, username, blurry, bad feet, single color, ((((ugly)))), (((duplicate))), ((morbid)), " +
            "((mutilated)), (((tranny))), (((trans))), (((trannsexual))), (hermaphrodite),((poorly drawn face)), (((mutation))), (((deformed))), ((ugly)), blurry, " +
            "((bad anatomy)), (((bad proportions))), ((extra limbs)), (((disfigured))), (bad anatomy), gross proportions, (malformed limbs), ((missing arms)), " +
            "(missing legs), (((extra arms))), (((extra legs))), mutated hands,(fused fingers), (too many fingers), (((long neck))), (bad body perspect:1.1), (((nsfw))), ";

        private const string NegativePromptAbyss = "nsfw, (worst quality, low quality:1.4), (lip, nose, tooth, rouge, lipstick, eyeshadow:1.4), (blush:1.2), " +
            "(jpeg artifacts:1.4), (depth of field, bokeh, blurry, film grain, chromatic aberration, lens flare:1.0), (1boy, abs, muscular, rib:1.0), greyscale, " +
            "monochrome, dusty sunbeams, trembling, motion lines, motion blur, emphasis lines, text, title, logo, signature, ";

        private const string NegativePrompt = NegativePromptAnything + NegativePromptAbyss;

        private const string BasicBasePrompt = "<lora:pastelMixStylizedAnime_pastelMixLoraVersion:0.15>, " +
            "<lora:roluaStyleLora_r:0.15>,<lora:V11ForegroundPlant_V11:0.25>, masterpiece, best quality, ";

        private const string BasicSinglePrompt = BasicBasePrompt + "1girl, solo, ";
        private const string BasicTwinPrompt = BasicBasePrompt + "2girl, ";

        private static readonly Dictionary<string, double> basicStyleWeight = new()
        {
            { "jk", 0.6 },
            { "萝莉", 0.6 },
            { "Q版", 0.6 },
            { "衬衫", 0.6 },
            { "白裙", 0.6 },
            { "泳装", 0.6 },
            { "ol", 0.6 },
            { "lo", 0.55 },
            { "女仆", 0.6 },
            { "旗袍", 0.6 },
            { "水墨", 0.6 },
            { "机甲", 0.55 },
            { "原版", 0.6 },
            { "立绘", 0.6 },
            { "电锯", 0.55 },
            { "日常", 0.6 },
            { "浴衣", 0.6 },
            { "臭脚", 0.6 },
            { "抱枕", 0.6 },
            { "油画", 0.3 },
            { "天使", 0.6 },
            { "猫娘", 0.6 },
        };

        private static readonly HashSet<string> disableExtraStyle = new() { "抱枕" };


        private static readonly Dictionary<string, List<string>> promptMap = new()
        {
            { "jk", new () {
                "plant, <lora:miki-v2+v3:-w->, jk, school uniform, ",
                "jk, plant, <lora:miki-v2+v3:-w->, jk, school uniform, ",
                "jk, <lora:miki-v2+v3:-w->, school uniform, ",
                "<lora:miki-v2+v3:-w->, jk, school uniform, ",
            } },
            { "萝莉", new () {
                "plant, (loli), <lora:miki-v2+v3:-w->, js, loli, ",
                "(loli), <lora:miki-v2+v3:-w->, js, loli, ",
                "(loli), <lora:miki-v2+v3:-w->, js, mesugaki, loli, ",
                "plant, (loli), <lora:miki-v2+v3:-w->, js, mesugaki, loli, ",
            } },
            { "Q版", new () {
                "plant, (loli), <lora:miki-v2+v3:-w->, (chibi), loli, ",
                "(loli), <lora:miki-v2+v3:-w->, js, (chibi), loli, ",
            } },
            { "衬衫", new () {
                "<lora:miki-v2+v3:-w->, shirt, ",
                "<lora:miki-v2+v3:-w->, shirt, cardigan, ",
            } },
            { "白裙", new () {
                "<lora:miki-v2+v3:-w->, (white dress), skirt, off-shoulder dress, bare shoulders, bag, ",
                "<lora:miki-v2+v3:-w->, (white dress), skirt, off-shoulder dress, bare shoulders, ",
                "<lora:miki-v2+v3:-w->, (white dress), strap slip, off-shoulder dress, bare shoulders, ",
            } },
            { "泳装", new () {
                "<lora:miki-v2+v3:-w->, school swimsuit, ",
                "<lora:miki-v2+v3:-w->, one-piece swimsuit, ",
                "<lora:miki-v2+v3:-w->, side-tie bikini bottom, ",
            } },
            { "ol", new () {
                "office, (office lady),<lora:miki-v2+v3:-w->, (office lady), ",
                "office, (office lady),<lora:hipoly3DModelLora_v10:0.3>, <lora:miki-v2+v3:-w->, (office lady), ",
                "office, (office lady),<lora:miki-v2+v3:-w->, (office lady), blazer, cardigan, ",
                "office, (office lady),<lora:hipoly3DModelLora_v10:0.3>, <lora:miki-v2+v3:-w->, (office lady), blazer, cardigan, ",
            } },
            { "lo", new() {
                "<lora:miki-v2+v3:-w->, (gothic lolita), lolita fashion, ",
                "(loli), <lora:miki-v2+v3:-w->, (gothic lolita, lolita fashion), chibi, loli, ",
                "(loli), <lora:miki-v2+v3:-w->, (gothic lolita, lolita fashion), mesugaki, loli, ",
            } },
            { "女仆", new() {
                "<lora:miki-v2+v3:-w->, maid, maid headdress, maid apron, ",
            } },
            { "旗袍", new() {
                "<lora:miki-v2+v3:-w->, ",
                "<lora:miki-v2+v3:-w->, chinese, ",
            } },
            { "水墨", new() {
                "<lora:miki-v2+v3:-w->, (ink painting), illustration, (Chinese ink painting)," +
                "(Ink dyeing), (watercolor), (Chinese Brush Painting), (Chinese style), ink background, petals, (soaked), (flowing)"
            } },
            { "浴衣", new() {
                "<lora:miki-v2+v3:-w->, japanese kimono, obi, ",
                "<lora:miki-v2+v3:-w->, japanese kimono, ",
                "<lora:miki-v2+v3:-w->, japanese yukata, obi, ",
                "<lora:miki-v2+v3:-w->, japanese yukata, ",
            } },
            { "机甲", new() {
                "<lora:miki-v2+v3:-w->, kabuto, holding tantou, (machine:1.2),false limb, prosthetic weapon, ",
                "<lora:miki-v2+v3:-w->, kabuto, (machine:1.2),false limb, prosthetic weapon, ",
                "<lora:miki-v2+v3:-w->, kabuto, (machine:1.2),false limb, prosthetic weapon, ",
                "<lora:miki-v2+v3:-w->, (mecha:1.2), (machine:1.2), mecha clothes",
                "<lora:miki-v2+v3:-w->, (mecha:1.2), (machine:1.2), sliver bodysuit",
                "<lora:miki-v2+v3:-w->, (mecha:1.2), (machine:1.2), beautiful detailed sliver dragon armor",
            } },
            { "电锯", new() {
                "<lora:miki-v2+v3:-w->, (machine:1.2), (blood), (chainsaw man:1.2), (lolipop chainsaw:1.2), (holding chainsaw:1.2), ",
            } },
            { "原版", new() {
                "<lora:miki-v2+v3:-w->, ",
            } },
            { "立绘", new() {
                "<lora:miki-v2+v3:-w->, [(white background:1.5),::5], hexagon, mid shot, full body, <lora:gachaSplashLORA_gachaSplash31:1>, ",
            } },
            { "臭脚", new()
            {
                "<lora:miki-v2+v3:-w->, sneakers, black legwear, black thighhighs, (full body), ",
                "<lora:miki-v2+v3:-w->, sneakers, white legwear, white thighhighs, (full body), ",
            } },
            { "日常", new() {
                "<lora:miki-v2+v3:-w->, ",
                "<lora:miki-v2+v3:-w->, ",
            } },
            { "抱枕", new() {
                "<lora:miki-v2+v3:-w->, dakimakura, (lie on the bed), (white bed sheet background)",
                "<lora:miki-v2+v3:-w->, dakimakura, top view, (lie on the bed), (white bed sheet background)",
                "<lora:miki-v2+v3:-w->, dakimakura, plan view, (lie on the bed), (white bed sheet background)",
                "<lora:miki-v2+v3:-w->, dakimakura, on back, (lie on the bed), (white bed sheet background), sheet grab, panty pull, bra pull",
                "<lora:miki-v2+v3:-w->, dakimakura, on back, white bed sheet background, sheet grab, panty pull, bra pull",
                "<lora:miki-v2+v3:-w->, dakimakura, on back, (white bed sheet background), sheet grab, panty pull, bra pull",
            } },
            { "油画", new() {
                "(illustration), ((impasto)), ((oil painting)), (classicism), <lora:miki-v2+v3:-w->, (portrait), rembrandt lighting, brown background, detailed face, picture frame, "
            } },
            { "猫娘", new() {
                "<lora:miki-v2+v3:-w->, (cat ears), (cat tail), cat girl, white legwear, white thighhighs, (full body), ",
                "<lora:miki-v2+v3:-w->, (cat ears), (cat tail), cat girl, black legwear, black thighhighs, (full body), ",
            } },
            { "天使", new() {
                "<lora:miki-v2+v3:-w->, (angel), (angel wings), ",
                "<lora:miki-v2+v3:-w->, (angel), (angel wings), halo",
            } },
        };

        private const int CD = 30;

        private static bool HasCategory(string category)
        {
            return promptMap.ContainsKey(category);
        }

        private static readonly List<string> categories = promptMap.Keys.ToList();

        private static readonly Dictionary<string, List<string>> mainStocks = new()
        {
            { "ol", new(){
                "white thighhighs", "black thighhighs", "black stocking", "white stocking", "black pantyhose", "white pantyhose",
                "black legwear",  "high heels",  "thigh boots",  "torn legwear",  "high heel boots",  "brown legwear",  
                "toeless legwear",  "print legwear",  "lace-trimmed legwear",  "bodystocking",  "legwear under shorts",  
                "pantylines",  "alternate legwear",  "seamed legwear",  "ribbed legwear",  "fur-trimmed legwear",  "strappy heels",  
                "ankle socks",  "see-through legwear",  "fine fabric emphasis",  "legwear garter",  "stiletto heels",  "back-seamed legwear",  
                "boots removed",  "two-tone legwear",  "bow legwear",  "leg cutout",
            } },
        };

        private static readonly Dictionary<string, List<string>> mainClothes = new()
        {
            { "ol", new(){
                "office uniform", "police uniform", "military uniform", "business suit", "dress shirt", "shirt",
            } },
            { "旗袍", new() {
                "(red china dress)", "(white china dress)", "(blue china dress)", "(cyan china dress)",
                "(yellow china dress)", "(red chinese clothes)", "(white chinese clothes)", "(blue chinese clothes)",
                "(cyan chinese clothes)", "(yellow chinese clothes)", "(black china dress)", "(black chinese clothes)",
                "(red cheongsam)", "(white cheongsam)", "(blue cheongsam)", "(cyan cheongsam)", "(yellow cheongsam)",
                "(black cheongsam)",
            } },
            { "白裙", new()
            {
                "straw hat", "paper fan", ",",
            } },
            { "浴衣", new()
            {
                "aerial fireworks", "sparkler", "paper fan",
                "pink kimono", "red kimono", "blue kimono", "white kimono", "purple kimono"
            } },
            { "日常", new()
            {
                "full body, white skirt, short dress, bag, red jacket, thighhighs, black legwear, shoes",
                "full body, white skirt, short dress, bag, sandals",
                "upper body, white skirt, short dress, bag",
                "pants, shirt, holding phone",
                "pants, shirt, jacket",
                "upper body, pants, shirt, holding phone",
                "upper body, pants, shirt, jacket",
                "full body, pants, shirt, holding phone, sneakers",
                "full body, pants, shirt, jacket, sneakers",
                "upper body, bare legs, sweatshirt, sneakers",
                "full body, bare legs, sweatshirt, sneakers",
                "upper body, hoodie, sneakers",
                "full body, bare legs, hoodie, sneakers",
                "full body, sportswear, sports bra, shorts, sneakers",
                "white shirt, long skirt, boots",
                "white shirt, long skirt",
                "full body, short pants, sports bra, jacket, sneakers",
                "short pants, sports bra, jacket",
            } },
            { "臭脚", new()
            {
                "sweatshirt, short pants",
                "full body, sweatshirt, short pants, on stomach",
                "sportswear, sports bra, short pants",
                "full body, sportswear, sports bra, short pants, on stomach",
                "shirt, short pants",
                "full body, shirt, short pants, on stomach",
            } },
        };

        private static readonly List<string> behaviours = new()
        {
            "lap pillow", "hug", "fighting stance", "princess carry",
            "standing", "mimikaki", "sweat", "wet", "sleeping", "bathing",
        };

        private static readonly List<string> actions= new()
        {
            "eye contact","symmetrical hand pose","symmetrical docking","back-to-back","leaning forward",
            "leg hug","indian style","yokozuwari","wariza","seiza","sitting","all fours","bent over",
            "kneeling","straddle","squatting","on stomach","lying","looking back",
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
            "sigh","serious","screaming","scared","sad","raised eyebrow","pout",
            "orgasm","open mouth","nervous","naughty face","light smile","licking","gloom depressed",
            "frown","fang","expressionless","embarrassed","drunk","drooling","disgust","confused",
            "annoyed","ahegao","open mouth","lips","eyelid pull",
            "food on face","wink","dark persona","shy",
        };

        private static readonly List<string> rolePalys = new()
        {
            "yuri","milf","kemonomimi mode","minigirl","furry","magical girl","vampire","devil","monster","angel",
            "elf","fairy","mermaid","nun", "dancer, ballet dress","doll","cheerleader","waitress","maid","miko","witch",
        };

        private static readonly List<string> scenes = new()
        {
            "cityscape", "scenery", "rose", "petal", "coconut tree", 
            "pine tree", "maple tree", "cypress", "fruit tree", "treehouse", "twig", "tree stump", "tree shade", 
            "rainy days", "night", "full moon", "on a desert", "in a meadow", "in hawaii", "in winter", "in autumn", 
            "in summer", "in spring", "futon", "sofa", "train", "locker room", "hallway", "telephone pole", "underwater", 
            "ruins", "magic circle", "pentagram background", "christmas tree", "cherry tree", "building", "bathtub", 
            "starry sky", "sea", "mountain","gothic architecture","office, mountain in window",
            "castle","dormitory","street, plant","beach, sunshine","flowers meadows, sunshine",
            "forest,outline,fountain,shop,outdoors,east asian architecture,greco-roman architecture,restaurant,shanty town,slum", 
            "cyberpunk, city, kowloon, rain", "starry sky,clusters of stars,starry sky,glinting stars", "floating sakura", 
            "violet background", "glinting stars", "floating white feathers", "aurora", "snowflakes", "snowfield", "cafe", 
            "grassland", "blue sky with clouds", "electricity", "rain", "blur background", "farm", "red moon", "battlefield", 
            "alpine", "coffee house", "lawn", "on bed", "fireworks", "isekai cityscape", "flying butterfly", "beach background", 
            "dungeon background", "airport background", "cityscape", "space background", "beige background", "simple pattern background", 
            "shooting star", "cherry blossoms", "colorful startrails", "white background", "silhouette", "gradient background", 
            "sunburst background", "starry sky,clusters of stars", "snow", "sunlight on the desk", 
            "building,rain,neon lights,cumulonimbus,moon"
        };

        private static readonly List<string> views = new()
        {
            "thigh focus", "navel focus", "breast focus", "back focus", "armpit focus", "horizontal view angle", "full-body shot",
            "focus on face", "looking at viewer", "from below", "from above", "dynamic angle", "dynamic pose", "back", "full body",
            "bust", "profile", "upper body", "full body",
        };

        private static readonly List<string> skys = new()
        {
            "morning", "sunset", "sunrise", "sunshine", "night, night sky, moon", "night, night sky, dark moon", "night, night sky, red moon",
            "blue sky", "cloudy sky", "night, night sky, starry sky", "night, night sky", "gradient sky", "night, night sky, star",
            "night, night sky, cloudy sky", "morning, cloudy sky", "sunset, cloudy sky", "sunrise, cloudy sky", "sunshine, cloudy sky", 
        };

        private static readonly List<string> seasons = new()
        {
            "spring", "summer", "autumn", "winter",
            "spring", "summer", "autumn",
        };

        private static readonly List<string> emojis = new()
        {
            "🧝🏻‍♂️", "🧝🏻‍♀️", "🧙🏻‍♂️", "🧙🏻‍♀️", "🧐", "🦸🏻‍♂️", "🦸🏻‍♀️", "🥺", "🥴", "🤵🏻", "🤬", "🤡", "🤕", "🤓", "🙁", "😷", "😵", "😴",
            "😳", "😲", "😱", "😰", "😯", "😭", "😫", "😪", "😩", "😨", "😤", "😣", "😢", "😡", "😠", "😟", "😞", "😛", "😕",
            "😓", "😎", "😍", "😋", "😈", "😇", "🕵🏻‍♂️", "💩", "💀", "👿", "👾", "👽", "👻", "👺", "👹", "👷🏻‍♂️", "👷🏻‍♀️", "👰🏻", "👮🏻‍♂️",
            "👮🏻‍♀️", "👩🏻‍🚒", "👩🏻‍🚀", "👩🏻‍🔬", "👩🏻‍🔧", "👩🏻‍💼", "👩🏻‍💻", "👩🏻‍🏭", "👩🏻‍🏫", "👩🏻‍🎨", "👩🏻‍🎤", "👩🏻‍🎓", "👩🏻‍🍳", "👩🏻‍🌾", "👩🏻‍⚖️",
            "👩🏻‍⚕️", "👩🏻‍✈️", "👨🏻‍🚒", "👨🏻‍🚀", "👨🏻‍🔬", "👨🏻‍🔧", "👨🏻‍💻", "👨🏻‍🏭", "👨🏻‍🏫", "👨🏻‍🎨", "👨🏻‍🎤", "👨🏻‍🎓", "👨🏻‍🍳", "👨🏻‍🌾", "👨🏻‍⚖️",
            "👨🏻‍⚕️", "👨🏻‍✈️", "☹️", "☠️"
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

        private static string SuffixOf(string style, string character)
        {
            List<string> suffixs = new();
            if (characterSuffix.TryGetValue(character, out var styleSuffixs))
            {
                if (styleSuffixs.TryGetValue(style, out var styleSuffix))
                {
                    suffixs.Add(styleSuffix);
                }
            }
            if (mainStocks.TryGetValue(style, out var stocks))
            {
                suffixs.Add(RandomOf(stocks));
            }
            if (mainClothes.TryGetValue(style, out var clothes))
            {
                suffixs.Add(RandomOf(clothes));
            }
            return $"{string.Join(", ", suffixs)}, ";
        }

        private static List<(string, int, int)> resoultions = new()
        {
            ("中横版", 768, 432),
            ("中竖版", 432, 768),
            ("中等宽", 576, 576),
            ("中超宽", 1024, 256),
            ("中超长", 256, 1024),
            ("大横板", 1024, 576),
            ("大竖版", 576, 1024),
            ("大等宽", 768, 768),
            ("大超宽", 1280, 432),
            ("大超长", 432, 1280),
        };

        private const int LargeSize = 768 * 768;

        private static (string, string, double, int, int, int) GetPrompt(string style, string character, int sizeRange = 0, bool useCustomWeight = false, double weightOffset = 0.0)
        {
            if (!promptMap.TryGetValue(style, out var prompts))
            {
                var randCategory = RandomOf(categories);
                style = randCategory!;
                prompts = promptMap[style]!;
            }

            var lora = characterLore[character];
            var weight = basicStyleWeight[style] + weightOffset;
            if (characterWeightOffset.TryGetValue(character, out var offset)) {
                weight += offset;
            }
            var loraSuffix = useCustomWeight ? ":MIDD" : "";
            var main = RandomOf(prompts)
                .Replace(DefaultLora, lora)
                .Replace("-w-", $"{weight}{loraSuffix}");

            var direction = sizeRange > 0 ? sizeRange - 1 : (random.Next(100) switch
            {
                <= 20 => 0,
                > 20 and < 70 => 1,
                _ => 2,
            });
            var (directionHint, width, height) = resoultions[direction];
            directionHint = $"{directionHint}({width * 2.5}*{height * 2.5})";
            var enableExtra = !disableExtraStyle.Contains(style);

            var prefix = characterPrefix.GetValueOrDefault(key: character) ?? "";
            //var emo = RandomOf(emotions);
            var view = enableExtra && random.Next(100) > 75 ? RandomOf(views) : "";
            var cfgScale = random.Next(100) > 40 ? random.Next(45, 100) / 10D : 8;
            var steps = random.Next(100) > 60 ? random.Next(24, 46) : 30;
            var sky = enableExtra && random.Next(100) > 75 ? RandomOf(skys) : "";
            var season = enableExtra && random.Next(100) > 75 ? RandomOf(seasons) : "";
            var scene = enableExtra && random.Next(100) > 75 ? RandomOf(scenes) : "";
            var suffix = SuffixOf(style, character);

            if (style == "原版")
            {
                return (
                    $"{BasicSinglePrompt}{prefix}{main}, {view}, {sky}, {season}, {suffix}, ",
                    $"生成词: ({main})\n视角: {view}\n专属附加词：{suffix}\n天空: {sky}\n" +
                    $"季节: {season}\ncfg_scale={cfgScale},step={steps},{directionHint}",
                    cfgScale, steps, width, height);
            }

            var hair = RandomOf(hairStyles);
            var extra = "";

            if (random.Next(10) >= 9)
            {
                var behaviour = RandomOf(behaviours);
                var action = RandomOf(actions);
                var rp = RandomOf(rolePalys);
                var emoji = RandomOf(emojis);

                extra = $"[{behaviour}], [{action}], [{emoji}], [{rp}], ";
            }

            return (
                $"{BasicSinglePrompt}{prefix}{main}, {hair}, {extra}, {view}, {scene}, {sky}, {season}, {suffix}, ",
                $"生成词: ({main})\n视角: {view}\n发型: {hair}\n场景:{scene}\n附加词: {extra}\n专属附加词：{suffix}\n天空: {sky}\n" +
                $"季节: {season}\ncfg_scale={cfgScale},step={steps},{directionHint}",
                cfgScale, steps, width, height);
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
            { "恶魔弥", new() { "139528984" } },
            { "弥", new() { "139528984" } },
            { "老版弥", new() { "139528984" } },
            { "真", new() { "139528984" } },
            { "悠", new() { "139528984" } },
            { "侑", new() { "139528984" } },
            { "老版侑", new() { "139528984" } },
            { "炉", new() { "139528984" } },
            { "老版炉", new() { "139528984" } },
            { "毬", new() { "139528984", "972488523" } },
            { "岁", new() { "139528984" } },
        };

        private static readonly Dictionary<string, string> characterLore = new()
        {
            { "恶魔弥", "miki-4.0-v1" },
            { "老版弥", "miki-v2+v3" },
            { "弥", "miki-2.0+3.0-v1-hd" },
            { "真", "mahiru-v2" },
            { "悠", "yuaVirtuareal_v20A5" },
            { "侑", "KiyuuVirtuareal_v20" },
            { "老版侑", "Kiyuu_" },
            { "炉", "kaoru-1.0-v1-hd" },
            { "老版炉", "kaoru-1.0-v5" },
            { "毬", "akumaria-2.0+3.0-hd-v1" },
            { "岁", "suiVirtuareal_suiVr" },
        };

        private static readonly Dictionary<string, double> characterWeightOffset = new()
        {
            { "弥", 0.1 },
        };

        private static readonly Dictionary<string, string> characterPrefix = new()
        {
            { "老版弥", "purple eyes, black hair, [purple streaked hair], (small breast), " },
            { "弥", "purple eyes, black hair, [purple streaked hair], (small breast), " },
            { "真", "yellow eyes, red hair, small breast, pointy ears, (small breast), (flat chest), " },
            { "悠", "(light blue eyes), black hair ribbon, silver hair, blue streaked hair, vr-yua, " },
            { "侑", "(white pink hair), (blue streaked hair), (cat ear headphone), <lora:Kiyuu_:0.15>, (small breast), " },
            { "老版侑", "(white pink hair), (blue streaked hair), (cat ear headphone), (small breast), " },
            { "炉", "yellow eyes, (pink to blue gradient hair), (gradient hair), (small breast), white colored eyelashes, hat, " },
            { "老版炉", "yellow eyes, (pink to blue gradient hair), (gradient hair), (small breast),  white colored eyelashes, hat, " },
            { "毬", "red eyes, silver hair, red streaked hair, (square pupils), (small breast), " },
            { "岁", "red eyes, silver hair, red hair robbon, (small breast), " },
            { "恶魔弥", "yellow eyes, black hair, purple horns, purple streaked hair, small breast, purple hair ornament, " }
        };

        private static readonly string demon = "demon girl, demon tail, demon wings, demon horns, ";
        private static readonly Dictionary<string, string> demonSuffix = new() {
            { "jk", demon}, { "萝莉", demon}, { "Q版", demon}, { "衬衫", demon}, { "白裙", demon}, { "泳装", demon},
            { "ol", demon}, { "lo", demon}, { "女仆", demon}, { "旗袍", demon}, { "水墨", demon}, { "机甲", demon},
            { "立绘", demon}, { "电锯", demon}, { "日常", demon}, { "浴衣", demon}, { "臭脚", demon}, { "抱枕", demon}, { "油画", demon},
        };

        private static readonly Dictionary<string, Dictionary<string, string>> characterSuffix = new()
        {
            { "弥", new() {
                { "原版", "thighhighs, black hair, long hair, fish hair ornament, blush, hairclip, shoes, black_legwear, zettai ryouiki, uniform, school uniform, grey_jacket, white shirt, bowtie, " },
                { "白裙", "sandals, blush, collarbone, white footwear, miki bag summer, miki v2, " },
            } },
            { "炉", new() {
                { "原版", "deep blue shorts, white shirt, (white capelet), [[hat]], black tie, long sleeves, " },
            } },
            { "老版炉", new() {
                { "原版", "deep blue shorts, white shirt, (white capelet), [[hat]], black tie, long sleeves, " },
            } },
            { "真", new(demonSuffix)
            {
                { "原版", $"{demon}, twintails, bat hair ornament, cape, white shirt, white legwear, stuffed toy, stuffed animal toy, beret, gloves, bangs, hat, " },
            } },
            { "恶魔弥", new() {
                { "原版", "ero thletic leotard, ero thletic leotar, sleeves pass wrists, bare shoulders, off shoulder," +
                    "collarbone, fishnet legwear, long sleeves, thighhighs, black fishnets," +
                    "garter straps, black footwear, demon tail, heart ear ornament, black shorts," +
                    " bangs, " },
            } },
            { "毬", new(demonSuffix) {
                { "原版", demon}, 
            } },
        };

        private static MessageChain getHelpMsg(string groupId) {
            var availableCharacters = characterLore.Keys
                .Where(c => characterLimit[c].Contains(groupId));
            return new MessageChainBuilder()
                                .Plain($"指令有2分钟的CD，使用'[原话][画幅]!来张[风格][人物]'生成（需要英文符号）\n\n" +
                                $"例子：!来张随机弥\n可用人物:{string.Join(',', availableCharacters)}\n" +
                                $"可用风格\n：随机,{string.Join(',', categories)}\n\n" +
                                $"原画大小：[ml] m-中等 l-大(该选项会增加15秒CD)\n" +
                                $"画幅选项：[hvswl] h,v 横,纵 / s 等宽 / w,l 超宽,超长\n").Build();
        }

        private static int NumbericHvs(string hvs)
        {
            return hvs switch
            {
                "h" => 1,
                "v" => 2,
                "s" => 3,
                "w" => 4,
                "l" => 5,
                "mh" => 1,
                "mv" => 2,
                "ms" => 3,
                "mw" => 4,
                "ml" => 5,
                "lh" => 6,
                "lv" => 7,
                "ls" => 8,
                "lw" => 9,
                "ll" => 10,
                _ => 0,
            };
        }

        private static (string, string, int, bool) ParseCommand(string raw)
        {
            var match = MatchRegex().Matches(raw).FirstOrDefault();
            if (match is null) {
                return ("", "", 0, false);
            }
            return (match.Result("$2"), match.Result("$3"), NumbericHvs(match.Result("$1")), match.Result("$4") == "@");
        }

        private static (string, string, int, bool) ParseManualCommand(string raw)
        {
            var match = MatchManualRegex().Matches(raw).FirstOrDefault();
            if (match is null) {
                return ("", "", 0, false);
            }
            return (match.Result("$2"), match.Result("$3"), NumbericHvs(match.Result("$1")), match.Result("$4") == "@");
        }

        private struct TwinArg
        {
            public int Size { get; set; }
            public string TwinStyle { get; set; }
            public string FirstStyle { get; set; }
            public string SecondStyle { get; set; }
            public string First { get; set; }
            public string Second { get; set; }
            public bool WeightControl { get; set; }
            public string Couple { get; set; }
        }

        private static bool ParseTwinCommand(string raw, out TwinArg arg)
        {
            var match = MatchRegexTwin().Matches(raw).FirstOrDefault();
            arg = new TwinArg();
            if (match is null)
            {
                return false;
            }

            arg.Couple = match.Result("$1") == "竖" ? "上下" : "左右";
            arg.Size = NumbericHvs(match.Result("$2"));
            arg.TwinStyle = match.Result("$3");
            arg.FirstStyle = match.Result("$4");
            arg.First = match.Result("$5");
            arg.SecondStyle = match.Result("$6");
            arg.Second = match.Result("$7");
            arg.WeightControl = match.Result("$8") == "@";
            return true;
        }

        private async ValueTask ProcessManual(Mirai.Net.Data.Shared.Group group, string raw, CancellationToken token)
        {
            var (character, prompt, _, customWieght) = ParseManualCommand(raw);
            
            var prefix = characterPrefix.GetValueOrDefault(character) ?? "";
            var weight = 0.6 + characterWeightOffset.GetValueOrDefault(character);
            var lora = characterLore.GetValueOrDefault(character) ?? "";
            var loraSuffix = customWieght ? ":MIDD" : "";
            var fullPrompt = $"{BasicSinglePrompt}, {prefix}, <lora:{lora}:{weight}{loraSuffix}>, {prompt}";
            await miraiService.SendMessageToGroup(group, token, GetGenerateMsg(fullPrompt).ToArray());

            var ret = await Request(fullPrompt, token: token);
            await SendImage(group, prompt, ret, token);
        }

        private static readonly Dictionary<string, string> twinStyles = new()
        {
            { "贴贴", " yuri, side-by-side, hand in hand, breast to breast, " },
            { "啵嘴", " yuri, (kiss), <lora:animeKisses_v1:0.8>, tongue kiss, " },
            { "啵贴", " yuri, side-by-side, hand in hand, breast to breast, (kiss), <lora:animeKisses_v1:0.8>, tongue kiss,  " },
            { "对视", " yuri, holding hands, looking at another, eye contact, facing another, " }
        };

        private static readonly List<string> randomTwinStyles = new() { "啵嘴", "贴贴", "啵贴" };

        private async ValueTask ProcessTwin(Mirai.Net.Data.Shared.Group group, TwinArg twinArg, CancellationToken token)
        {
            var (promptFirst, extraFirst, cfg_scale, steps, width, height) = GetPrompt(twinArg.FirstStyle, twinArg.First, twinArg.Size, true);
            var (promptSecond, extraSecond, _, _, _, _) = GetPrompt(twinArg.SecondStyle, twinArg.Second, twinArg.Size, true);

            promptFirst = promptFirst[BasicSinglePrompt.Length..];
            promptSecond = promptSecond[BasicSinglePrompt.Length..];
            if (twinArg.TwinStyle == "随机")
            {
                twinArg.TwinStyle = RandomOf(randomTwinStyles);
            }
            if (!twinStyles.TryGetValue(twinArg.TwinStyle, out var twinStylePrompt))
            {
                twinStylePrompt = "";
            }


            var fullPrompt = $"yuri, 2girl, {BasicTwinPrompt}{twinStylePrompt},\n" +
                $"AND yuri, masterpiece, best quality, 2girl, {promptFirst},{twinStylePrompt}\n" +
                $"AND yuri, masterpiece, best quality, 2girl, {promptSecond},{twinStylePrompt}";
            await miraiService.SendMessageToGroup(group, token, GetGenerateMsg($"左：{twinArg.FirstStyle}{twinArg.First}\n" +
                $"右：{twinArg.SecondStyle}{twinArg.Second}\n" +
                $"人物位置：{twinArg.Couple}\n" +
                $"构图Prompt: {twinStylePrompt}").ToArray());

            var ret = await Request((arg) =>
            {
                DefaultArg(arg);
                arg.CfgScale = cfg_scale;
                arg.Steps = steps;
                arg.Size(width, height);
                arg.EnabledComposableLora();

                arg.EnableLatentCouple(twinArg.Couple == "上下" ? Extensions.LatentCouple.TopDown(steps) : Extensions.LatentCouple.LeftRight(steps));

                arg.EnableHiresScale(2.5f);
                arg.Prompt = fullPrompt;
            }, token);
            await SendImage(group, "", ret, token);
        }

        private async ValueTask<Ret> Request(Action<Text2Img> argHandler, CancellationToken token = default)
        {

            latestGenerateAt = DateTimeOffset.Now + TimeSpan.FromMinutes(1);
            isCdHintShown = false;
            var arg = new Text2Img();
            argHandler(arg);
            var content = JsonContent.Create(arg);
            logger.LogInformation("request = {}", await content.ReadAsStringAsync(token));
            var res = await httpClient.PostAsync($"{WebUiEndpoint}", content, token);
            var isLarge = arg.Width * arg.Height >= LargeSize;
            var cdModify = isLarge ? 15 : -20;
            latestGenerateAt = DateTimeOffset.Now + TimeSpan.FromSeconds(cdModify);
            try
            {
                var body = await res.Content.ReadFromJsonAsync<Ret>(cancellationToken: token);
                logger.LogInformation("response = {}", body.info);
                return body;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "catched after access AI!");
                throw;
            }
        }

        private static void DefaultArg(Text2Img arg)
        {
            arg.Sampler = "DPM++ 2M Karras";
            arg.EnableLoraBlockWeight();
            arg.NegativePrompt = NegativePrompt;
            arg.EnableHiresScale(2.5f);
        }

        private async ValueTask<Ret> Request(string prompt, double cfg_scale = 8, int steps = 26, int width = 768, int height = 432, CancellationToken token = default)
        {

            latestGenerateAt = DateTimeOffset.Now + TimeSpan.FromMinutes(1);
            isCdHintShown = false;
            logger.LogInformation("prompt: {}", prompt);
            return await Request((arg) =>
            {
                arg.Prompt = prompt;
                arg.CfgScale = cfg_scale;
                arg.Steps = steps;
                arg.Width = width;
                arg.Height = height;
                DefaultArg(arg);
            }, token);
        }

        private async ValueTask SendImage(Mirai.Net.Data.Shared.Group group, string prompt, Ret body, CancellationToken token)
        {
            var info = JsonSerializer.Deserialize<Info>(body.info);
            logger.LogInformation("生成成功，种子:{}", info.seed);
            if (body.images.Count != 0)
            {
                var imageBase64 = body.images[0];
                AiImageColorAdjustUtility.TryAdjust(prompt, imageBase64, out var adjustedImage);
                List<MessageBase> messages = new()
                {
                    new PlainMessage()
                    {
                        Text = $"生成成功，种子:{info.seed}"
                    },
                    new ImageMessage()
                    {
                        Base64 = adjustedImage,
                    },
                };

                await miraiService.SendMessageToGroup(group, token, messages.ToArray());
            }
        }

        private static readonly List<string> Lucky = new()
        {
            "大吉",
            "吉", "吉",
            "中吉", "中吉", "中吉",
            "小吉", "小吉", "小吉", "小吉", 
            "末吉", "末吉", "末吉",
            "凶", "凶", 
            "大凶"
        };
        private static string StringDayOfWeek(DayOfWeek dow)
        {
            return dow switch
            {
                DayOfWeek.Monday => "周一",
                DayOfWeek.Tuesday => "周二",
                DayOfWeek.Wednesday => "周三",
                DayOfWeek.Thursday => "周四",
                DayOfWeek.Friday => "周五",
                DayOfWeek.Saturday => "周六",
                DayOfWeek.Sunday => "周日",
                _ => "?",
            };
        }
        private async ValueTask SendLuckyImage(Mirai.Net.Data.Shared.Group group, string name, string uid, string prompt, string weather, Ret body, CancellationToken token)
        {
            var info = JsonSerializer.Deserialize<Info>(body.info);
            logger.LogInformation("生成成功，种子:{}", info.seed);
            if (body.images.Count != 0)
            {
                var imageBase64 = body.images[0];
                var today = DateTime.Now;
                var todayStr = $"{today.Year}年{today.Month}月{today.Day}日 {StringDayOfWeek(today.DayOfWeek)}";
                AiImageColorAdjustUtility.TryAppendLucky(prompt, todayStr, RandomOf(Lucky), name, weather, imageBase64, out var adjustedImage);
                List<MessageBase> messages = new()
                {
                    new AtMessage() { Target = uid },
                    new PlainMessage()
                    {
                        Text = $"生成成功，种子:{info.seed}"
                    },
                    new ImageMessage()
                    {
                        Base64 = adjustedImage,
                    },
                };

                await miraiService.SendMessageToGroup(group, token, messages.ToArray());
            }
        }

        private static readonly Dictionary<string, int> characterRandomWeight = new()
        {
            { "恶魔弥", 25 },
            { "弥", 50 },
            { "真", 10 },
            { "悠", 5 },
            { "侑", 10 },
            { "炉", 10 },
            { "毬", 10 },
            { "岁", 5 },
        };
        private static readonly int Max = characterRandomWeight.Values.Sum();
        private static readonly List<string> allCharacters = characterRandomWeight.Keys.ToList();

        private async Task Idle(Mirai.Net.Data.Shared.Group group, string requireCharacter = "随机", string requireStyle = "随机", CancellationToken token = default)
        {
            while (!token.IsCancellationRequested)
            {
                var character = requireCharacter;
                if (character == "随机")
                {
                    var rand = random.Next(Max) + 1;
                    foreach (var currCharacter in allCharacters)
                    {
                        var weight = characterRandomWeight[currCharacter];
                        if (weight > rand)
                        {
                            character = currCharacter;
                            break;
                        }
                        rand -= weight;
                    }
                }

                await _lock.WaitAsync(token);
                try
                {
                    if (IsColdingDown())
                    {
                        continue;
                    }
                    var (prompt, extra, cfg_scale, steps, width, height) = GetPrompt(requireStyle, character);
                    await miraiService.SendMessageToGroup(group, token, GetGenerateMsg($"{extra}").ToArray());
                    logger.LogInformation("prompt: {}", prompt);

                    var body = await Request(prompt, cfg_scale, steps, width, height, token);
                    await SendImage(group, prompt, body, token);
                }
                finally
                {
                    _lock.Release();
                }
                await Task.Delay(TimeSpan.FromMinutes(5), token);
            }
        }

        private readonly SemaphoreSlim _lock = new(1);
        private readonly Dictionary<string, CancellationTokenSource> controller = new();

        private readonly Dictionary<string, List<string>> allowCharacter = new()
        {
            { "314503649", new() { "弥", "弥", "弥", "真", "侑", "悠" }  },
            { "139528984", allCharacters },
            { "972488523", new() { "毬"} },
        };

        private async ValueTask Dequeue(CancellationToken token)
        {
            await foreach (var msg in messageQueue.Reader.ReadAllAsync(token))
            {
                var group = msg.Sender.Group;

                foreach (var rawMsg in msg.MessageChain)
                {
                    if (rawMsg is PlainMessage plain)
                    {
                        if (plain.Text == "!help")
                        {
                            await miraiService.SendMessageToGroup(group, token, getHelpMsg(group.Id).ToArray());
                        }
                        if (plain.Text.StartsWith("!idle") && msg.Sender.Id == "644676751")
                        {
                            if (controller.TryGetValue(group.Id, out var value))
                            {
                                await miraiService.SendMessageToGroup(group, token, new MessageBase[]
                                {
                                    new PlainMessage() { Text = "已关闭本群的闲置跑图功能。" },
                                });
                                using var csc = value;
                                csc.Cancel();
                                controller.Remove(group.Id);
                            }
                            else
                            {
                                var csc = CancellationTokenSource.CreateLinkedTokenSource(token);
                                controller.Add(group.Id, csc);

                                var (reqStyle, reqCharacter, size, _) = ParseCommand(plain.Text);
                                reqStyle = reqStyle == "" ? "随机" : reqStyle;
                                reqCharacter = reqCharacter == "" ? "随机" : reqCharacter;

                                _ = Idle(group, reqCharacter, reqStyle, csc.Token);

                                await miraiService.SendMessageToGroup(group, token, new MessageBase[]
                                {
                                    new PlainMessage() { Text = $"已开启本群的闲置跑图功能。\n角色：{reqCharacter}，风格：{reqStyle}" },
                                });
                            }
                            continue;
                        }
                        if (plain.Text.StartsWith("!每日运势") || plain.Text.StartsWith("!今日运势") || plain.Text.StartsWith("!抽签") || plain.Text.StartsWith("!运势")
                            || plain.Text.StartsWith("！每日运势") || plain.Text.StartsWith("！今日运势") || plain.Text.StartsWith("！抽签") || plain.Text.StartsWith("！运势"))
                        {
                            await _lock.WaitAsync(token);
                            try
                            {
                                if (!File.Exists("lucky.json"))
                                {
                                    await File.WriteAllTextAsync("lucky.json", "{}", token);
                                }
                                var dict = await JsonSerializer.DeserializeAsync<Dictionary<string, DateTime>>(File.OpenRead(("lucky.json")), cancellationToken: token) ?? new();
                                if (dict.TryGetValue(msg.Sender.Id, out var lastDate))
                                {
                                    if (DateTime.Now.Date - lastDate == TimeSpan.Zero)
                                    {
                                        continue;
                                    }
                                    dict.Remove(msg.Sender.Id);
                                }
                                var category = RandomOf(categories);
                                var luckyCharacter = allowCharacter.ContainsKey(msg.GroupId) ? RandomOf(allowCharacter[msg.GroupId]) : "弥";
                                await miraiService.SendMessageToGroup(group, token, new MessageBase[]
                                {
                                    new AtMessage() { Target = msg.Sender.Id },
                                    new PlainMessage() { Text = $" {category}{luckyCharacter}正在为你计算今天的运势~" },
                                });
                                var (prompt, extra, cfg_scale, steps, width, height) = GetPrompt(category, luckyCharacter, 2);
                                logger.LogInformation("prompt: {}", prompt);
                                var imgTask = Request(prompt, cfg_scale, steps, width, height, token);
                                var weatherTask = plain.Text.Contains('+') switch
                                {
                                    true => weatherService.SearchTodayForecast(plain.Text[(plain.Text.IndexOf('+') + 1)..]),
                                    _ => Task.FromResult<(Location, Daily)>((null!, null!)),
                                };
                                var (loc, weather) = await weatherTask;
                                logger.LogInformation("{} weather: {}", loc, weather);
                                var weatherStr = (weather != null) switch
                                {
                                    true => 
                                            $"{loc.Adm2} {loc.Name} · {weather.TextDay} · {weather.TempMin}~{weather.TempMax}℃ \n" +
                                            $"🌅{weather.Sunrise} 🌇{weather.Sunset} 💧{weather.Humidity} 🍃{weather.WindSpeedDay}级 {weather.WindDirDay}",
                                    _ => "",
                                };
                                var body = await imgTask;
                                await SendLuckyImage(group, msg.Sender.Name, msg.Sender.Id, prompt, weatherStr, body, token);

                                dict.Add(msg.Sender.Id, DateTime.Now.Date);
                                logger.LogInformation("dict size = {}", dict.Count);
                                await File.WriteAllTextAsync("lucky.json", JsonSerializer.Serialize(dict), token);
                            }
                            finally
                            {
                                _lock.Release();
                            }
                            continue;
                        }
                        if (plain.Text.StartsWith("!!"))
                        {
                            if (msg.Sender.Id == "644676751")
                            {
                                await ProcessManual(group, plain.Text, token);
                            }
                            continue;
                        }
                        if (ParseTwinCommand(plain.Text, out var twinArg))
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
                                await ProcessTwin(group, twinArg, token);
                            }
                            continue;
                        }
                        var (style, character, sizeRange, useCustomWeigth) = ParseCommand(plain.Text);
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
                            await _lock.WaitAsync(token);
                            try
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
                                    isCdHintShown = false;
                                    var (prompt, extra, cfg_scale, steps, width, height) = GetPrompt(style, character, sizeRange, useCustomWeigth);
                                    await miraiService.SendMessageToGroup(group, token, GetGenerateMsg(extra).ToArray());
                                    logger.LogInformation("prompt: {}", prompt);
                                    try
                                    {
                                        var body = await Request(prompt, cfg_scale, steps, width, height, token);
                                        await SendImage(group, prompt, body, token);
                                    }
                                    catch (Exception ex)
                                    {
                                        logger.LogError(ex, "access AI errored!");
                                        return;
                                    }
                                }
                            }
                            finally
                            {
                                _lock.Release();
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

        [GeneratedRegex("([ml]?[hvslw]?)!来张(..)(.*?)(@?)$")]
        private static partial Regex MatchRegex();


        [GeneratedRegex("([横竖]?)([ml]?[hvslw]?)!双人(..)(..)(.*?)和(..)(.*?)(@?)$")]
        private static partial Regex MatchRegexTwin();


        [GeneratedRegex("([ml]?[hvslw]?)!!(.) (.*?)(@?)$")]
        private static partial Regex MatchManualRegex();
    }
}
