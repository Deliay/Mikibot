using System.Runtime.CompilerServices;
using MemeFactory.Core.Processing;
using MemeFactory.Core.Utilities;
using Microsoft.Extensions.Logging;
using Mikibot.Analyze.Bot.Images;
using Mikibot.Analyze.Generic;
using Mikibot.Analyze.MiraiHttp;
using Mirai.Net.Data.Messages;
using Mirai.Net.Data.Messages.Concretes;
using Mirai.Net.Data.Messages.Receivers;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Transforms;

namespace Mikibot.Analyze.Bot;

public class ImageProcessorService(
    IQqService qqService,
    ILogger<ImageProcessorService> logger,
    ILogger<MemeCommandHandler> memeLogger)
    : MiraiGroupMessageProcessor<ImageProcessorService>(qqService, logger)
{
    private readonly MemeCommandHandler memeCommandHandler = new(memeLogger);

    private readonly Dictionary<string, string> _knownCommandMapping = new()
    {
        { "shoot", "射" },
        { "jerk", "打" },
        { "punch", "👊" },
        { "chushou", "触手" },
        { "fupunch", "敷拳"}
    };

    private readonly HashSet<string> _hiddenCommands = ["shoot", "jerk"];
    private readonly Dictionary<string, List<Image?>> SavedImages = [];

    private static Func<Image, Func<Frame, Point>> GetLayoutFunction(string layoutStr)
    {
        return layoutStr switch
        {
            "lt" => Layout.LeftTop,
            "lc" => Layout.LeftCenter,
            "lb" => Layout.LeftBottom,
            "tc" => Layout.TopCenter,
            "c" => Layout.Center,
            "bc" => Layout.BottomCenter,
            "rt" => Layout.RightTop,
            "rc" => Layout.RightCenter,
            "rb" => Layout.RightBottom,
            _ => throw new Memes.AfterProcessError(nameof(GetLayoutFunction), $"不支持的位置函数: {layoutStr}")
        };
    }

    private Memes.Factory AppendMeme(string groupId)
    {
        return AppendCore;

        async IAsyncEnumerable<Frame> AppendCore(IAsyncEnumerable<Frame> frames, string argument, CancellationToken token)
        {
            if (!Memes.TryParseNamed<int>(argument, "id", out var idx) || idx < 0)
                throw new Memes.AfterProcessError(nameof(AppendMeme), "木有指定id，使用指令//存 存储一张图");
            
            if (!SavedImages.TryGetValue(groupId, out var images)
                || images.Count == 0
                || idx >= images.Count)
                throw new Memes.AfterProcessError(nameof(AppendMeme), "木有已存储表情");
            
            var image = images[idx];
            
            if (image is null) 
                throw new Memes.AfterProcessError(nameof(AppendMeme), "最大存100个表情，这个超过了被释放掉了😭");

            var frameList = await frames.ToListAsync(token);
            
            foreach (var frame in frameList)
            {
                yield return frame;
            }

            var size = frameList.Count > 0 ? frameList[0].Image.Size : image.Size;
            var shouldResize = size != image.Size;
            await foreach (var extraFrame in image.ExtractFrames().WithCancellation(token))
            {
                if (shouldResize)
                {
                    extraFrame.Image.Mutate(x => x.Resize(size, new BicubicResampler(), true));
                }
                    
                yield return extraFrame with { Sequence = extraFrame.Sequence + frameList.Count };
            }
        }
    }
    
    private Memes.Factory GetMemePaster(string groupId)
    {
        return Paste; 
        
        IAsyncEnumerable<Frame> Paste(IAsyncEnumerable<Frame> frames, string argument, CancellationToken token)
        {
            if (!Memes.TryParseNamed<int>(argument, "id", out var idx) || idx < 0)
                throw new Memes.AfterProcessError(nameof(Paste), "木有指定id，使用指令//存 存储一张图");
            
            if (!SavedImages.TryGetValue(groupId, out var images)
                || images.Count == 0
                || idx >= images.Count)
                throw new Memes.AfterProcessError(nameof(Paste), "木有已存储表情");
            
            var image = images[idx];
            
            if (image is null) 
                throw new Memes.AfterProcessError(nameof(Paste), "最大存100个表情，这个超过了被释放掉了😭");

            if (!Memes.TryParseNamed<string>(argument, "layout", out var layoutStr))
                layoutStr = "lb";

            var layoutFn = GetLayoutFunction(layoutStr);
            
            return frames.FrameBasedZipSequence(image.ExtractFrames().LcmExpand(cancellationToken: token),
                Composers.Draw(Resizer.Auto, layoutFn), cancellationToken: token);
        }
    }
    
    protected override ValueTask PreRun(CancellationToken token)
    {
        memeCommandHandler.RegisterStaticMethods(typeof(Memes));
        
        var autoComposeMemeFolders = Directory.EnumerateDirectories(Path.Combine("resources", "meme", "auto"));
        foreach (var autoComposeMemeFolder in autoComposeMemeFolders)
        {
            var triggerWord = Path.GetFileName(autoComposeMemeFolder)!;
            Logger.LogInformation("Add {} meme composer, trigger word: {}", autoComposeMemeFolder, triggerWord);
            if (_hiddenCommands.Contains(triggerWord))
            {
                memeCommandHandler.Register(triggerWord, Memes.AutoCompose(autoComposeMemeFolder));
            }
            else
            {
                memeCommandHandler.Register(triggerWord, "", Memes.AutoCompose(autoComposeMemeFolder));
            }

            if (!_knownCommandMapping.TryGetValue(triggerWord, out var knownCommand)) continue;
            
            if (_hiddenCommands.Contains(triggerWord))
                memeCommandHandler.Register(knownCommand, Memes.AutoCompose(autoComposeMemeFolder));
            else 
                memeCommandHandler.Register(knownCommand, "", Memes.AutoCompose(autoComposeMemeFolder));
        }
        
        memeCommandHandler.RegisterGroupCommand("贴", "id(n),layout(lt/lc/lb/tc/c/bc/rt/rc/rb)", GetMemePaster);
        memeCommandHandler.RegisterGroupCommand("合", "id(n)", AppendMeme);
        
        Logger.LogInformation("Total {} meme processor has been registered.", memeCommandHandler.MemeHelpers.Count);
        return ValueTask.CompletedTask;
    }

    private async IAsyncEnumerable<MessageBase> GetImageInfoAsync(IEnumerable<ImageMessage> messages,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var imageMessage in messages)
        {
            using var image = await QqService.ReadImageAsync(imageMessage.Url, cancellationToken);
            var info = image.Width + "*" + image.Height;
            if (image.Metadata.DecodedImageFormat is { } decoder)
            {
                info += $",{decoder.Name} ({decoder.FileExtensions.FirstOrDefault() ?? "未知"})";
            }

            if (image.Metadata.TryGetGifMetadata(out var gif))
            {
                info += "\n-----gif-----";
                info += "\n重复: " + (gif.RepeatCount == 0 ? "无限" : gif.RepeatCount + "次");
                info += "\n帧间隔: " + string.Join(',', image.Frames
                    .Select(f => f.Metadata.GetGifMetadata().FrameDelay)
                    .Aggregate(new List<(int delay, int count)>(), (lst, currentDelay) =>
                    {
                        if (lst.Count == 0) lst.Add((currentDelay, 1));
                        else
                        {
                            if (lst[^1].delay == currentDelay) lst[^1] = lst[^1] with { count = lst[^1].count + 1 };
                            else lst.Add((currentDelay, 1));
                        }

                        return lst;
                    })
                    .Select(p => $"{p.delay * 10}ms * {p.count}f"));
            }

            yield return new PlainMessage(info);
        }
    }

    private async IAsyncEnumerable<MessageBase> StorageImage(string groupId, List<ImageMessage> imageMessages,
        [EnumeratorCancellation] CancellationToken token)
    {
        if (!SavedImages.TryGetValue(groupId, out var imageQueue))
            SavedImages.Add(groupId, imageQueue = []);

        if (imageMessages.Count == 0) yield break;
            
        if (imageQueue.Count > 100)
        {
            using var dispose = imageQueue[0];
            imageQueue[0] = null;
        }
        
        foreach (var imageMessage in imageMessages)
        {
            var image = await QqService.ReadImageAsync(imageMessage.Url, token);

            var idx = imageQueue.Count;
            imageQueue.Add(image);
            
            yield return new PlainMessage($"已添加id={idx}");
        }
    }
    
    protected override async ValueTask Process(GroupMessageReceiver message, CancellationToken token = default)
    {
        var groupId = message.Sender.Group.Id;
        var msg = message.MessageChain.GetPlainMessage().Trim();

        Logger.LogInformation("Meme processor, processing: {}", msg);
        if (!msg.StartsWith('/')) return;

        var imageMessages = message.MessageChain
            .Concat(message.MessageChain.OfType<QuoteMessage>().SelectMany(q => q.Origin))
            .OfType<ImageMessage>()
            .ToList();

        if (msg == "/信息")
        {
            if (imageMessages.Count == 0) return;
            var infoMsg = await GetImageInfoAsync(imageMessages, token).ToArrayAsync(token);
            if (infoMsg.Length != 0)
            {
                await QqService.SendMessageToSomeGroup([groupId], token, infoMsg);
            }
            return;
        }
        
        if (msg.StartsWith("//帮"))
        {
            var helpStr = string.Join(" | ", memeCommandHandler.MemeHelpers
                .Select(p => p.Value is { Length: > 0 }
                    ? $"/{p.Key}:{p.Value}"
                    : $"/{p.Key}"));
            Logger.LogInformation("Sending help text to group {}, content: {}", groupId, helpStr);
            await QqService.SendMessageToSomeGroup([groupId], token, new PlainMessage(helpStr));
            return;
        }

        if (msg.StartsWith("//存"))
        {
            var replyMsg = await this.StorageImage(groupId, imageMessages, token).ToArrayAsync(token);
            if (replyMsg.Length == 0) return;

            await QqService.SendMessageToSomeGroup([groupId], token, replyMsg);
            
            return;
        }
        
        var processor = memeCommandHandler.GetComposePipeline(msg, groupId);

        if (processor is null) return;

        if (imageMessages.Count is 0 or > 10)
        {
            Logger.LogInformation("Too many or no image in message chain! Count: {}",
                imageMessages.Count);
            return;
        }

        var result = await imageMessages.ToAsyncEnumerable()
            .SelectMany(RunImage)
            .ToArrayAsync(token);
        
        await QqService.SendMessageToSomeGroup([groupId], token, result);
        
        return;
        async IAsyncEnumerable<MessageBase> RunImage(ImageMessage imageMessage)
        {
            logger.LogInformation("Processing image, url: {}", imageMessage.Url);
            MemeResult? result = null;
            Exception? exception = null;
            List<Memes.AfterProcessError> knownErrors = null; 
            try
            {
                using var image = await QqService.ReadImageAsync(imageMessage.Url, token);
                using var seq = await image.ExtractFrames().ToSequenceAsync(token);
                var (frames, errors) = processor(seq, token);
                var frameDelay = !msg.Contains("间隔") ? 6 : -1;
                result = await frames.AutoComposeAsync(frameDelay, token);
                knownErrors = errors;
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            if (exception is not null)
            {
                Logger.LogError(exception, "An exception was thrown when processing");
                yield return new PlainMessage($"出错🌶！{exception.Message}");
                yield break;
            }
            if (result is null) yield break;
            using var imageResult = result.Value;
            
            var frameCount = imageResult.Image.Frames.Count;
            if (frameCount > 1000)
            {
                yield return new PlainMessage($"生成了{frameCount}帧超过1000了，发不出来┑(￣Д ￣)┍");
            }
            else
            {
                yield return new ImageMessage() { Base64 = await imageResult.ToDataUri(token) };
            }
            if (knownErrors is { Count: > 0 })
            {
                yield return new PlainMessage(string.Join('\n', knownErrors.Select(v => v.Message)));
            }
        }
        
    }
}