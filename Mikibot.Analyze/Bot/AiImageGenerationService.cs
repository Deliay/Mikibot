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

        private const string NegativePromptAnything = "lowres, bad anatomy, bad hands, text, error, missing fingers, extra digit, fewer digits, cropped, worst quality, " +
            "low quality, normal quality, jpeg artifacts, signature, watermark, username, blurry, (((look_at_viewer))),((((extra fingers)))),((look_at_viewer)), " +
            "mutated hands, ((poorly drawn hands)), paintings, sketches, (worst quality:2), (low quality:2), (normal quality:2), lowres, normal quality, ((monochrome)), " +
            "((grayscale)), skin spots, acnes, skin blemishes, age spot, glans, nipples, (((necklace))), (worst quality, low quality:1.2), watermark, username, " +
            "signature, text, multiple breasts, lowres, bad anatomy, bad hands, text, error, missing fingers, extra digit, fewer digits, cropped, worst quality, " +
            "low quality, normal quality, jpeg artifacts, signature, watermark, username, blurry, bad feet, single color, ((((ugly)))), (((duplicate))), ((morbid)), " +
            "((mutilated)), (((tranny))), (((trans))), (((trannsexual))), (hermaphrodite),((poorly drawn face)), (((mutation))), (((deformed))), ((ugly)), blurry, " +
            "((bad anatomy)), (((bad proportions))), ((extra limbs)), (((disfigured))), (bad anatomy), gross proportions, (malformed limbs), ((missing arms)), " +
            "(missing legs), (((extra arms))), (((extra legs))), mutated hands,(fused fingers), (too many fingers), (((long neck))), (bad body perspect:1.1), (((nsfw)))";

        private const string NegativePromptAbyss = "nsfw, (worst quality, low quality:1.4), (lip, nose, tooth, rouge, lipstick, eyeshadow:1.4), (blush:1.2), " +
            "(jpeg artifacts:1.4), (depth of field, bokeh, blurry, film grain, chromatic aberration, lens flare:1.0), (1boy, abs, muscular, rib:1.0), greyscale, " +
            "monochrome, dusty sunbeams, trembling, motion lines, motion blur, emphasis lines, text, title, logo, signature, ";

        private const string NegativePrompt = NegativePromptAbyss;

        private const string BasicPrompt = "<lora:pastelMixStylizedAnime_pastelMixLoraVersion:0.25>, " +
            "<lora:roluaStyleLora_r:0.25>,<lora:V11ForegroundPlant_V11:0.3>, " +
            "masterpiece, best quality, 1girl, solo, ";

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
            { "机甲", 0.55 },
            { "原版", 0.6 },
            { "电锯", 0.55 },
            { "日常", 0.6 },
            { "浴衣", 0.6 },
            { "臭脚", 0.6 },
        };


        private static readonly Dictionary<string, List<string>> promptMap = new()
        {
            { "jk", new () {
                "school, plant, <lora:miki-v2+v3:-w->, jk, school uniform, ",
                "engine room, plant, <lora:miki-v2+v3:-w->, jk, school uniform, ",
                "laboratory, jk, <lora:miki-v2+v3:-w->, school uniform, ",
                "street, plant, <lora:miki-v2+v3:-w->, jk, school uniform, ",
                "stairs, plant, <lora:miki-v2+v3:-w->, jk, school uniform, ",
                "school, plant, <lora:miki-v2+v3:-w->, jk, school uniform, cardigan, ",
                "engine room, plant, <lora:miki-v2+v3:-w->, jk, school uniform, cardigan, ",
                "laboratory, <lora:miki-v2+v3:-w->, jk, school uniform, cardigan, ",
                "street, plant, <lora:miki-v2+v3:-w->, jk, school uniform, cardigan, ",
                "stairs, plant, <lora:miki-v2+v3:-w->, jk, school uniform, cardigan, ",
            } },
            { "萝莉", new () {
                "school, plant, (loli), <lora:miki-v2+v3:-w->, loli, ",
                "laboratory, (loli), <lora:miki-v2+v3:-w->, js, loli, ",
                "street, plant, (loli), <lora:miki-v2+v3:-w->, js, loli, ",
                "stairs, plant, (loli), <lora:miki-v2+v3:-w->, js, loli, ",
                "school, plant, (loli), <lora:miki-v2+v3:-w->, mesugaki, loli, ",
                "laboratory, (loli), <lora:miki-v2+v3:-w->, js, mesugaki, loli, ",
                "street, plant, (loli), <lora:miki-v2+v3:-w->, js, mesugaki, loli, ",
                "stairs, plant, (loli), <lora:miki-v2+v3:-w->, js, mesugaki, loli, ",
            } },
            { "Q版", new () {
                "school, plant, (loli), <lora:miki-v2+v3:-w->, (chibi), loli, ",
                "laboratory, (loli), <lora:miki-v2+v3:-w->, js, (chibi), loli, ",
                "street, plant, (loli), <lora:miki-v2+v3:-w->, js, (chibi), loli, ",
                "stairs, plant, (loli), <lora:miki-v2+v3:-w->, js, (chibi), loli, ",
                "school, plant, (loli), <lora:miki-v2+v3:-w->, mesugaki, (chibi), loli, ",
                "laboratory, (loli), <lora:miki-v2+v3:-w->, js, mesugaki, (chibi), loli, ",
                "street, plant, (loli), <lora:miki-v2+v3:-w->, js, mesugaki, (chibi), loli, ",
                "stairs, plant, (loli), <lora:miki-v2+v3:-w->, js, mesugaki, (chibi), loli, ",
            } },
            { "衬衫", new () {
                "mountain, lake, forest, <lora:miki-v2+v3:-w->, shirt, plant, cardigan, ",
                "laboratory, <lora:miki-v2+v3:-w->, shirt, ",
                "mountain, forest, plant, <lora:miki-v2+v3:-w->, shirt, cardigan, ",
                "castle, <lora:miki-v2+v3:-w->, shirt, plant, ",
                "street, <lora:miki-v2+v3:-w->, shirt, plant, ",
                "dormitory, plant, <lora:miki-v2+v3:-w->, shirt, cardigan, ",
            } },
            { "白裙", new () {
                "mountain, <lora:miki-v2+v3:-w->, (white dress), skirt, off-shoulder dress, bare shoulders, miki bag summer, ",
                "castle, <lora:miki-v2+v3:-w->, (white dress), skirt, off-shoulder dress, bare shoulders, ",
                "dormitory, <lora:miki-v2+v3:-w->, (white dress), skirt, off-shoulder dress, bare shoulders, ",
                "street, plant, <lora:miki-v2+v3:-w->, (white dress), skirt, off-shoulder dress, bare shoulders, ",
                "street, plant, <lora:miki-v2+v3:-w->, (white dress), skirt, off-shoulder dress, bare shoulders, ",
                "beach, sunshine, <lora:miki-v2+v3:-w->, (white dress), skirt, off-shoulder dress, bare shoulders, ",
                "flowers meadows, sunshine, <lora:miki-v2+v3:-w->, (white dress), skirt, off-shoulder dress, bare shoulders, ",
                "mountain, <lora:miki-v2+v3:-w->, (white dress), strap slip, off-shoulder dress, bare shoulders, ",
                "castle, <lora:miki-v2+v3:-w->, (white dress), strap slip, off-shoulder dress, bare shoulders, ",
                "dormitory, <lora:miki-v2+v3:-w->, (white dress), strap slip, off-shoulder dress, bare shoulders, ",
                "street, plant, <lora:miki-v2+v3:-w->, (white dress), strap slip, off-shoulder dress, bare shoulders, ",
                "street, plant, <lora:miki-v2+v3:-w->, (white dress), strap slip, off-shoulder dress, bare shoulders, ",
                "beach, sunshine, <lora:miki-v2+v3:-w->, (white dress), strap slip, off-shoulder dress, bare shoulders, ",
                "flowers meadows, sunshine, <lora:miki-v2+v3:-w->, (white dress), strap slip, off-shoulder dress, bare shoulders, ",
            } },
            { "泳装", new () {
                "poolside, <lora:miki-v2+v3:-w->, school swimsuit, ",
                "beach, ocean, <lora:miki-v2+v3:-w->, school swimsuit, ",
                "poolside, <lora:miki-v2+v3:-w->, one-piece swimsuit, ",
                "beach, ocean, <lora:miki-v2+v3:-w->, one-piece swimsuit, ",
                "beach, ocean, <lora:miki-v2+v3:-w->, side-tie bikini bottom, ",
            } },
            { "ol", new () {
                "office, (office lady), mountain in window, <lora:miki-v2+v3:-w->, ",
                "office, laboratory, (office lady),<lora:miki-v2+v3:-w->, ",
                "office, dormitory, (office lady),<lora:miki-v2+v3:-w->, office, ",
                "office, dormitory, (office lady),<lora:hipoly3DModelLora_v10:0.3>, <lora:miki-v2+v3:-w->, (office lady), ",
                "office, laboratory, (office lady),<lora:hipoly3DModelLora_v10:0.3>, <lora:miki-v2+v3:-w->, (office lady), ",
                "office, (office lady), mountain in window, <lora:hipoly3DModelLora_v10:0.3>, <lora:miki-v2+v3:-w->, (office lady), ",
                "office, (office lady), <lora:miki-v2+v3:-w->, (office lady), blazer, cardigan, ",
                "office, laboratory, (office lady),<lora:miki-v2+v3:-w->, (office lady), blazer, cardigan, ",
                "office, dormitory, (office lady),<lora:miki-v2+v3:-w->, (office lady), blazer, cardigan, ",
                "office, dormitory, (office lady),<lora:hipoly3DModelLora_v10:0.3>, <lora:miki-v2+v3:-w->, (office lady), blazer, cardigan, ",
                "office, laboratory, (office lady),<lora:hipoly3DModelLora_v10:0.3>, <lora:miki-v2+v3:-w->, (office lady), blazer, cardigan, ",
                "office, mountain in window, (office lady),<lora:hipoly3DModelLora_v10:0.3>, <lora:miki-v2+v3:-w->, (office lady), blazer, cardigan, ",
            } },
            { "lo", new() {
                "gothic architecture, plant, <lora:miki-v2+v3:-w->, gothic lolita, lolita fashion, ",
                "gothic architecture, plant, (loli), <lora:miki-v2+v3:-w->, gothic lolita, lolita fashion, chibi, loli, ",
                "gothic architecture, plant, (loli), <lora:miki-v2+v3:-w->, gothic lolita, lolita fashion, mesugaki, loli, ",
                "gothic architecture, plant, (loli), <lora:miki-v2+v3:-w->, gothic lolita, lolita fashion, mesugaki, loli, ",
            } },
            { "女仆", new() {
                "dormitory, <lora:miki-v2+v3:-w->, maid, maid headdress, maid apron, ",
                "street, <lora:miki-v2+v3:-w->, maid, maid headdress, maid apron, ",
                "castle, <lora:miki-v2+v3:-w->, maid, maid headdress, maid apron, ",
                "mountain, <lora:miki-v2+v3:-w->, maid, maid headdress, maid apron, ",
                "forest, <lora:miki-v2+v3:-w->, maid, maid headdress, maid apron, ",
            } },
            { "旗袍", new() {
                "dormitory, <lora:miki-v2+v3:-w->, chinese, ",
                "chinese street, <lora:miki-v2+v3:-w->, chinese, ",
                "chinese mountain,, <lora:miki-v2+v3:-w->, chinese, ",
                "chinese forest, <lora:miki-v2+v3:-w->, chinese, lake, ",
                "dormitory, <lora:miki-v2+v3:-w->, chinese, ",
                "chinese street, <lora:miki-v2+v3:-w->, chinese, ",
                "chinese mountain, <lora:miki-v2+v3:-w->, chinese, ",
                "chinese forest, <lora:miki-v2+v3:-w->, chinese, lake, ",
            } },
            { "浴衣", new() {
                "dormitory, <lora:miki-v2+v3:-w->, japanese kimono, obi, ",
                "japanese street, <lora:miki-v2+v3:-w->, japanese kimono, ",
                "japanese mountain,, <lora:miki-v2+v3:-w->, japanese kimono, obi, ",
                "japanese forest, <lora:miki-v2+v3:-w->, japanese kimono, lake, ",
                "dormitory, <lora:miki-v2+v3:-w->, japanese yukata, obi, ",
                "japanese street, <lora:miki-v2+v3:-w->, japanese yukata, ",
                "japanese mountain, <lora:miki-v2+v3:-w->, japanese yukata, ",
                "japanese forest, <lora:miki-v2+v3:-w->, japanese yukata, lake, obi, ",
            } },
            { "机甲", new() {
                "<lora:miki-v2+v3:-w->, kabuto, holding tantou, (machine:1.2),false limb, prosthetic weapon, ",
                "<lora:miki-v2+v3:-w->, kabuto, (machine:1.2),false limb, prosthetic weapon, ",
                "<lora:miki-v2+v3:-w->, kabuto, (machine:1.2),false limb, prosthetic weapon, ",
                "<lora:miki-v2+v3:-w->, (mecha:1.2), (machine:1.2), ",
                "<lora:miki-v2+v3:-w->, (mecha:1.2), (machine:1.2), ",
            } },
            { "电锯", new() {
                "<lora:miki-v2+v3:-w->, cyberpunk, (machine:1.2), (blood), (chainsaw man:1.2), (lolipop chainsaw:1.2), (holding chainsaw:1.2), ",
                "<lora:miki-v2+v3:-w->, kabuto, (machine:1.2), (blood), (chainsaw man:1.2), (lolipop chainsaw:1.2), (holding chainsaw:1.2), ",
                "<lora:miki-v2+v3:-w->, dormitory, (machine:1.2), (blood), (chainsaw man:1.2), (lolipop chainsaw:1.2), (holding chainsaw:1.2), ",
                "<lora:miki-v2+v3:-w->, street, (machine:1.2), (blood), (chainsaw man:1.2), (lolipop chainsaw:1.2), (holding chainsaw:1.2), ",
                "<lora:miki-v2+v3:-w->, castle, (machine:1.2), (blood), (chainsaw man:1.2), (lolipop chainsaw:1.2), (holding chainsaw:1.2), ",
                "<lora:miki-v2+v3:-w->, mountain, (machine:1.2), (blood), (chainsaw man:1.2), (lolipop chainsaw:1.2), (holding chainsaw:1.2), ",
                "<lora:miki-v2+v3:-w->, office, (machine:1.2), (blood), (chainsaw man:1.2), (lolipop chainsaw:1.2), (holding chainsaw:1.2), ",
                "<lora:miki-v2+v3:-w->, forest, (machine:1.2), (blood), (chainsaw man:1.2), (lolipop chainsaw:1.2), (holding chainsaw:1.2), ",
                "<lora:miki-v2+v3:-w->, laboratory, (machine:1.2), (blood), (chainsaw man:1.2), (lolipop chainsaw:1.2), (holding chainsaw:1.2), ",
                "<lora:miki-v2+v3:-w->, beach, (machine:1.2), (blood), (chainsaw man:1.2), (lolipop chainsaw:1.2), (holding chainsaw:1.2), ",
            } },
            { "原版", new() {
                "dormitory, <lora:miki-v2+v3:-w->, ",
                "street, <lora:miki-v2+v3:-w->, ",
                "castle, <lora:miki-v2+v3:-w->, ",
                "mountain, <lora:miki-v2+v3:-w->, ",
                "forest,  <lora:miki-v2+v3:-w->, ",
                "office,  <lora:miki-v2+v3:-w->, ",
                "laboratory,  <lora:miki-v2+v3:-w->, ",
                "beach, <lora:miki-v2+v3:-w->, ",
            } },
            { "臭脚", new()
            {
                "<lora:miki-v2+v3:-w->, sneakers, black legwear, thighhighs",
                "<lora:miki-v2+v3:-w->, sneakers, white legwear, thighhighs",
                "<lora:miki-v2+v3:-w->, sneakers, black legwear, thighhighs",
            } },
            { "日常", new() {
                "<lora:miki-v2+v3:-w->, ",
                "<lora:miki-v2+v3:-w->, ",
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
            "sigh","serious","screaming","scared","sad","round teeth","raised eyebrow","pout","pain",
            "orgasm","open mouth","nervous","naughty face","light smile","licking","gloom depressed","fucked silly",
            "frown","fang","expressionless","embarrassed","drunk","drooling","disgust","confused","clenched teeth",
            "annoyed","ahegao","looking at viewer","open mouth","clenched teeth","lips","eyeball","eyelid pull",
            "food on face","wink","dark persona","shy"
        };

        private static readonly List<string> rolePalys = new()
        {
            "yuri","milf","kemonomimi mode","minigirl","furry","magical girl","vampire","devil","monster","angel",
            "elf","fairy","mermaid","nun", "dancer, ballet dress","doll","cheerleader","waitress","maid","miko","witch",
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
            "sunburst background", "starry sky,clusters of stars", "snow", "sunlight on the desk", 
            "building,rain,neon lights,cumulonimbus,moon"
        };

        private static readonly List<string> views = new()
        {
            "thigh focus", "navel focus", "breast focus", "back focus", "armpit focus", "horizontal view angle", "full-body shot",
            "focus on face", "looking at viewer", "from below", "from above", "dynamic angle", "dynamic pose", "back", "full body",
            "bust", "profile",
        };

        private static readonly List<string> skys = new()
        {
            "morning", "sunset", "sunrise", "sunshine", "night, night sky, moon", "night, night sky, dark moon", "night, night sky, red moon",
            "blue sky", "cloudy sky", "night, night sky, starry sky", "night, night sky", "gradient sky", "night, night sky, star",
            "night, night sky, cloudy sky",
        };

        private static readonly List<string> seasons = new()
        {
            "spring", "summer", "autumn", "winter",
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

        private static string suffixOf(string style, string character)
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

        private static (string, string, double, int, int, int) GetPrompt(string style, string character, int sizeRange = 0)
        {
            if (!promptMap.TryGetValue(style, out var prompts))
            {
                var randCategory = RandomOf(categories);
                style = randCategory!;
                prompts = promptMap[style]!;
            }

            var lora = characterLore[character];
            var weight = basicStyleWeight[style];
            if (characterWeightOffset.TryGetValue(character, out var offset)) {
                weight += offset;
            }
            var main = RandomOf(prompts)
                .Replace(DefaultLora, lora)
                .Replace("-w-", $"{weight}");

            var direction = (sizeRange > 0 ? sizeRange : random.Next(100)) switch
            {
                <= 20 => 1,
                > 20 and < 70 => 2,
                _ => 3,
            };
            var directionHint = direction switch
            {
                1 => $"横版({direction})",
                2 => $"等宽({direction})",
                _ => $"竖版({direction})",
            };
            var width = direction switch
            {
                1 => 768,
                2 => 512,
                _ => 432,
            };
            var height = direction switch
            {
                1 => 432,
                2 => 512,
                _ => 768,
            };
            var prefix = characterPrefix.GetValueOrDefault(character) ?? "";
            var emo = RandomOf(emotions);
            var view = random.Next(100) > 30 ? "full body" : RandomOf(views);
            var cfgScale = random.Next(100) > 40 ? random.Next(45, 100) / 10D : 8;
            var steps = random.Next(100) > 60 ? random.Next(24, 46) : 30;
            var sky = RandomOf(skys);
            var season = RandomOf(seasons);
            var suffix = suffixOf(style, character);
            var scene = RandomOf(scenes);

            if (style == "原版")
            {
                return (
                    $"{BasicPrompt}{prefix}{main}({emo}), {view}, ({sky}), ({season}), {suffix}, ",
                    $"生成词: {main}\n视角: {view}\n表情: {emo}\n专属附加词：{suffix}\n天空: {sky}\n" +
                    $"季节: {season}\ncfg_scale={cfgScale},step={steps},{directionHint}",
                    cfgScale, steps, width, height);
            }

            var hair = RandomOf(hairStyles);
            var extra = "";

            if (random.Next(2) >= 1)
            {
                var behaviour = RandomOf(behaviours);
                var action = RandomOf(actions);
                //var rp = RandomOf(rolePalys);
                var emoji = RandomOf(emojis);

                extra = $"({behaviour}), ({action}), ({emoji}), ";
            }

            return (
                $"{BasicPrompt}{prefix}{main}({emo}), {hair}, {extra}, {view}, ({scene}), ({sky}), ({season}), {suffix}, ",
                $"生成词: {main}\n视角: {view}\n发型: {hair}\n场景:{scene}\n表情: {emo}\n附加词: {extra}\n专属附加词：{suffix}\n天空: {sky}\n" +
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
            { "老弥", new() { "139528984" } },
            { "真", new() { "139528984" } },
            { "悠", new() { "139528984" } },
            { "侑", new() { "139528984" } },
            { "炉", new() { "139528984" } },
            { "毬", new() { "139528984", "972488523" } },
            { "岁", new() { "139528984" } },
        };

        private static readonly Dictionary<string, string> characterLore = new()
        {
            { "恶魔弥", "miki-4.0-v1" },
            { "老弥", "miki-v2+v3" },
            { "弥", "miki-2.0+3.0-v1-hd" },
            { "真", "mahiru-v2" },
            { "悠", "yuaVirtuareal_v20" },
            { "侑", "KiyuuVirtuareal_v20" },
            { "炉", "kaoru-1.0-v5" },
            { "毬", "akumaria" },
            { "岁", "suiVirtuareal_suiVr" },
        };

        private static readonly Dictionary<string, double> characterWeightOffset = new()
        {
            { "炉", 0.1 },
            { "弥", 0.1 },
        };

        private static readonly Dictionary<string, string> characterPrefix = new()
        {
            { "弥", "purple eyes, black hair, [purple streaked hair], (small breast), " },
            { "真", "yellow eyes, red hair, small breast, demon girl, demon tail, demon wings, small demon horns, pointy ears, (small breast), (flat chest), " },
            { "悠", "(light blue eyes), black hair ribbon, silver hair, blue streaked hair, vr-yua, " },
            { "侑", "(white pink hair), (blue streaked hair), (cat ear headphone), <lora:Kiyuu_:0.15>, (small breast), " },
            { "炉", "yellow eyes, (pink to blue gradient hair), (gradient hair), ahoge, (small breast), (flat chest), white colored eyelashes, (+ +), " },
            { "毬", "red eyes, silver hair, red streaked hair, demon girl, demon tail, demon wings, demon horns, square pupils, (small breast), " },
            { "岁", "red eyes, silver hair, red hair robbon, (small breast), " },
            { "恶魔弥", "yellow eyes, black hair, purple horns, purple streaked hair, small breast, purple hair ornament, " }
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
            { "真", new()
            {
                { "原版", "twintails, bat hair ornament, cape, white shirt, white legwear, stuffed toy, stuffed animal toy, beret, gloves, bangs, hat, " },
            } },
            { "恶魔弥", new() {
                { "原版", "ero thletic leotard, ero thletic leotar, sleeves pass wrists, bare shoulders, off shoulder," +
                    "collarbone, fishnet legwear, long sleeves, thighhighs, black fishnets," +
                    "garter straps, black footwear, demon tail, heart ear ornament, black shorts," +
                    " bangs, " },
            } },
        };

        private static MessageChain getHelpMsg(string groupId) {
            var availableCharacters = characterLore.Keys
                .Where(c => characterLimit[c].Contains(groupId));
            return new MessageChainBuilder()
                                .Plain($"指令有2分钟的CD，使用'!来张[风格][人物]'生成（需要英文括号）\n\n例子：!来张随机弥\n可用人物:{string.Join(',', availableCharacters)}\n可用风格\n：随机,{string.Join(',', categories)}").Build();
        }

        private static int numbericHvs(string hvs)
        {
            return hvs switch
            {
                "h" => 10,
                "v" => 90,
                "s" => 50,
                _ => 0,
            };
        }

        private static (string, string, int) ParseCommand(string raw)
        {
            var match = MatchRegex().Matches(raw).FirstOrDefault();
            if (match is null) {
                return ("", "", 0);
            }
            return (match.Result("$2"), match.Result("$3"), numbericHvs(match.Result("$1")));
        }

        private static (string, string, int) ParseManualCommand(string raw)
        {
            var match = MatchManualRegex().Matches(raw).FirstOrDefault();
            if (match is null) {
                return ("", "", 0);
            }
            return (match.Result("$2"), match.Result("$3"), numbericHvs(match.Result("$1")));
        }

        private async ValueTask ProcessManual(Mirai.Net.Data.Shared.Group group, string raw, CancellationToken token)
        {
            var (character, prompt, _) = ParseManualCommand(raw);
            
            var prefix = characterPrefix.GetValueOrDefault(character) ?? "";
            var weight = 0.6 + characterWeightOffset.GetValueOrDefault(character);
            var lora = characterLore.GetValueOrDefault(character) ?? "";
            
            var fullPrompt = $"{BasicPrompt}, {prefix}, <lora:{lora}:{weight}>, {prompt}";
            await miraiService.SendMessageToGroup(group, token, GetGenerateMsg(fullPrompt).ToArray());

            var ret = await Request(fullPrompt, token: token);
            await SendImage(group, prompt, ret, token);
        }

        private async ValueTask<Ret> Request(string prompt, double cfg_scale = 8, int steps = 26, int width = 768, int height = 432, CancellationToken token = default)
        {

            latestGenerateAt = DateTimeOffset.Now;
            isCdHintShown = false;
            logger.LogInformation("prompt: {}", prompt);
            var res = await httpClient.PostAsync($"{WebUiEndpoint}", JsonContent.Create(new
            {
                prompt,
                enable_hr = true,
                denoising_strength = 0.6,
                hr_scale = 2.5,
                hr_upscaler = "Latent",
                hr_second_pass_steps = 30,
                cfg_scale,
                steps,
                sampler_index = "DPM++ 2M Karras",
                width,
                height,
                negative_prompt = NegativePrompt,
            }), token);
            latestGenerateAt = DateTimeOffset.Now - TimeSpan.FromSeconds(10);
            try
            {
                var body = await res.Content.ReadFromJsonAsync<Ret>(cancellationToken: token);
                var info = JsonSerializer.Deserialize<Info>(body.info);
                logger.LogInformation("生成成功，种子: {}", info.seed);
                return body;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "catched after access AI!");
                logger.LogInformation(await res.Content.ReadAsStringAsync());
                throw;
            }
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

        private static readonly Dictionary<string, int> characterRandomWeight = new()
        {
            { "恶魔弥", 25 },
            { "弥", 50 },
            { "老弥", 25 },
            { "真", 10 },
            { "悠", 5 },
            { "侑", 10 },
            { "炉", 10 },
            { "毬", 10 },
            { "岁", 5 },
        };
        private static int Max = characterRandomWeight.Values.Sum();
        private static List<string> allCharacters = characterRandomWeight.Keys.ToList();

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
                        if (plain.Text.StartsWith("!idle"))
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

                                var (reqStyle, reqCharacter, size) = ParseCommand(plain.Text);
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
                        if (plain.Text.StartsWith("!!"))
                        {
                            if (msg.Sender.Id == "644676751")
                            {
                                await ProcessManual(group, plain.Text, token);
                            }
                            continue;
                        }
                        var (style, character, sizeRange) = ParseCommand(plain.Text);
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
                                    var (prompt, extra, cfg_scale, steps, width, height) = GetPrompt(style, character, sizeRange);
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

        [GeneratedRegex("([hvs]?)!来张(..)(.*)$")]
        private static partial Regex MatchRegex();

        
        [GeneratedRegex("([hvs]?)!!(.) (.*)$")]
        private static partial Regex MatchManualRegex();
    }
}
