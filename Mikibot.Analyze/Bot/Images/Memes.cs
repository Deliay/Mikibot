using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using MemeFactory.Core.Processing;
using MemeFactory.Core.Utilities;
using Mikibot.Analyze.Bot.Images.OpenCv;
using Mirai.Net.Data.Messages;
using NPOI.SS.Formula.Functions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Point = OpenCvSharp.Point;

namespace Mikibot.Analyze.Bot.Images;

public static class Memes
{
    public class AfterProcessError(string command, string msg) : Exception($"{command}: {msg}") {}

    public readonly record struct ComposeResult(IAsyncEnumerable<Frame> Frames, List<AfterProcessError> Errors)
    {
        public ComposeResult CombineError(ComposeResult other)
        {
            return this with { Errors = [..Errors.Concat(other.Errors)] };
        }
    }

    public delegate ComposeResult FactoryComposer(IAsyncEnumerable<Frame> seq,
        CancellationToken cancellationToken = default);

    public static FactoryComposer Handle((Factory factory, string argument) pair)
    {
        var (factory, argument) = pair;
        return (seq, token) =>
        {
            try
            {
                return new ComposeResult(factory(seq, argument, token), []);
            }
            catch (AfterProcessError e)
            {
                return new ComposeResult(seq, [e]);
            }
        };
    }
    
    public delegate IAsyncEnumerable<Frame> Factory(IAsyncEnumerable<Frame> seq, string arguments, CancellationToken cancellationToken = default);
    
    private static readonly FrameMerger DrawLeftBottom = Composers.Draw(Resizer.Auto, Layout.LeftBottom);
    private static readonly FrameMerger DrawRightCenter = Composers.Draw(Resizer.Auto, Layout.RightCenter);
    
    private static readonly Func<Frame, FrameProcessor> DrawLeftBottomSingle =
        (f) => Composers.Draw(f.Image, Resizer.Auto, Layout.LeftBottom);
    
    private static readonly Func<Frame, FrameProcessor> DrawRightCenterSingle =
        (f) => Composers.Draw(f.Image, Resizer.Auto, Layout.RightCenter);

    private static async IAsyncEnumerable<Frame> SequenceZip(
        IAsyncEnumerable<Frame> frames, string folder, int slowTimes = 1,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var sequence = await Frames
            .LoadFromFolderAsync(folder, "*.png", cancellationToken)
            .DuplicateFrame(slowTimes)
            .ToSequenceAsync(cancellationToken);
        
        using var baseSequence = await frames.ToSequenceAsync(cancellationToken);
        
        var merger = sequence.LcmExpand(-1, cancellationToken);
        var pipeline = baseSequence.FrameBasedZipSequence(merger, DrawLeftBottom, cancellationToken);
        await foreach (var frame in pipeline) yield return frame;
    }
    
    public static Factory AutoCompose(string folder, int slowTimes = 1)
    {
        return (image, msg, token) => SequenceZip(image, folder, slowTimes, token);
    }

    [MemeCommandMapping("","结婚")]
    public static Factory Marry() => MarryCore;
    private static async IAsyncEnumerable<Frame> MarryCore(IAsyncEnumerable<Frame> frames, string message,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var resources = await Frames.LoadFromFolderAsync(Path.Combine("resources", "meme", "marry"), "*.png", cancellationToken)
            .ToListAsync(cancellationToken);
        using var left = resources[0];
        using var right = resources[1];
        
        using var baseSequence = await frames.ToSequenceAsync(cancellationToken);

        var pipeline = baseSequence
            .EachFrame(DrawLeftBottomSingle(left), cancellationToken)
            .EachFrame(DrawRightCenterSingle(right), cancellationToken);
        
        await foreach (var frame in pipeline) yield return frame;
    }
    
    
    [MemeCommandMapping("[旋转次数]","旋转")]
    public static Factory Rotation()
    {
        return (seq, arguments, token) =>
        {
            if (!int.TryParse(arguments, out var size)) size = 5;
            if (size is < 1 or > 64) throw new AfterProcessError(nameof(Rotation), "不准转这么多😡(1-64)");
            return seq.Rotation(size, token);
        };
    }

    private static (int hor, int vert, int slidingTimes) ParseSlidingArgument(string argument)
    {
        var numStart = argument.FirstOrDefault(char.IsNumber);
        var slidingTimes = 16;
        if (numStart != 0)
        {
            var numEnd = argument.LastOrDefault(char.IsNumber);
            var numStartPos = argument.IndexOf(numStart);
            var numEndPos = argument.LastIndexOf(numEnd);
            var numStr = argument[numStartPos..(numEndPos + 1)];
            if (int.TryParse(numStr, out var parsedSlidingTimes))
            {
                if (parsedSlidingTimes is > 64 or < 1)
                    throw new AfterProcessError(nameof(ParseSlidingArgument), "不准滑那么多😡 (1-64)");
            }
        }
        var hor = argument.Contains('右') ? -1 : argument.Contains('左') ? 1 : 0;
        var vert = argument.Contains('下') ? -1 : argument.Contains('上') ? 1 : 0;
        if (hor == 0 && vert == 0) hor = 1;

        return (hor, vert, slidingTimes);
    }
    
    [MemeCommandMapping("[上下][左右]", "滑")]
    public static Factory Sliding()
    {
        return (seq, argument, token) =>
        {
            
            var (hor, vert, slidingTimes) = ParseSlidingArgument(argument);
            return seq.Sliding(-hor, -vert, slidingTimes, cancellationToken: token);
        };
    }
    
    [MemeCommandMapping("[上下][左右]", "轴也滑")]
    public static Factory SlideTimeline()
    {
        return (seq, argument, token) =>
        {
            var (hor, vert, slidingTimes) = ParseSlidingArgument(argument);
            return seq.TimelineSliding(hor, vert, slidingTimes, cancellationToken: token);
        };
    }

    [MemeCommandMapping("[毫秒] ", "间隔")]
    public static Factory FrameDelay()
    {
        return (seq, arguments, token) =>
        {
            var frameDelay = int.TryParse(arguments, out var inputDelay)
                ? inputDelay
                : throw new AfterProcessError(nameof(FrameDelay), "间隔必须是数字");
            return FrameDelayCore(seq, frameDelay / 10, token);
        };

        async IAsyncEnumerable<Frame> FrameDelayCore(IAsyncEnumerable<Frame> seq, int frameDelay,
            [EnumeratorCancellation] CancellationToken token = default)
        {
            using var result = await seq.AutoComposeAsync(frameDelay, token);
            await foreach (var extractFrame in result.Image.ExtractFrames().WithCancellation(token))
            {
                var gifFrameMetadata = extractFrame.Image.Frames.RootFrame.Metadata.GetGifMetadata();
                var srcMetadata = result.Image.Frames[extractFrame.Sequence].Metadata.GetGifMetadata();
                gifFrameMetadata.FrameDelay = srcMetadata.FrameDelay;
                yield return extractFrame;
            }
        }
    }
    
    [MemeCommandMapping("[次数]", "肚皮里擦特烦然么", "复制")]
    public static Factory DuplicateFrame()
        => (seq, arguments, _) => seq.DuplicateFrame(int.TryParse(arguments, out var times) ? times : 1);
    
    private static async IAsyncEnumerable<Frame> LoopCore(IAsyncEnumerable<Frame> seq, int times,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var sequence = await seq.ToSequenceAsync(cancellationToken);

        foreach (var frame in sequence.Loop(times))
        {
            yield return frame;
        }
    }

    [MemeCommandMapping("[次数(可选)]", "循环")]
    public static Factory Loop()
        => (seq, arguments, token) => LoopCore(seq, int.TryParse(arguments, out var times) ? times : 1, token);


    private static Predicate<Frame> RangeFrameFilter(string argument)
    {
        if (argument.Length < 2) return (_) => true;
        var numberStartedAt = char.IsNumber(argument[1]) ? 1 : 2;
        if (numberStartedAt == 2 && argument.Length < 3) return (_) => true;
        if (!int.TryParse(argument[numberStartedAt..], out var result)) return (_) => true;
        
        if (argument.StartsWith('%')) return (frame) => frame.Sequence % result == 0; 
        if (argument.StartsWith('<')) return (frame) => frame.Sequence < result; 
        if (argument.StartsWith('>')) return (frame) => frame.Sequence > result;
        if (argument.StartsWith("<=")) return (frame) => frame.Sequence <= result;
        if (argument.StartsWith(">=")) return (frame) => frame.Sequence >= result;
        if (argument.StartsWith('^')) return (frame) => frame.Sequence == 0;

        return (_) => true;
    }
    
    [MemeCommandMapping("[v] // (v - 垂直镜像, 其他值水平镜像)","镜像")]
    public static Factory Flip() => (seq, arguments, token) =>
    {
        var flipMode = arguments.Contains('v') ? FlipMode.Vertical : FlipMode.Horizontal;
        return seq.Flip(flipMode);
    };

    [MemeCommandMapping("[宽][x*][高]", "大小")]
    public static Factory Resize() => (seq, arguments, token) =>
    {
        var resolution = arguments.Contains('x') ? arguments.Split('x') : arguments.Split('*');
        if (resolution.Length != 2) return seq;
        
        var width = int.Parse(resolution[0]);
        var height = int.Parse(resolution[1]);
        
        if (width < 16 || height < 16) throw new AfterProcessError(nameof(Resize), "太小了(16-512)");
        if (width > 512 || height > 512) throw new AfterProcessError(nameof(Resize), "太大了(16-512)");

        return seq.Resize(new ResizeOptions()
        {
            Compand = true,
            Mode = ResizeMode.Stretch,
            PremultiplyAlpha = true,
            Size = new Size(width, height),
        });
    };

    [MemeCommandMapping("[#颜色]", "背景")]
    public static Factory BackgroundColor() => (seq, arguments, token) =>
    {
        if (!Color.TryParseHex(arguments, out var color))
            throw new AfterProcessError(nameof(BackgroundColor), $"颜色{arguments}解析失败");

        return seq.BackgroundColor(color);
    };

    [MemeCommandMapping("[#颜色] // 可选", "发光")]
    public static Factory Glow() => (seq, arguments, token) =>
    {
        if (!Color.TryParseHex(arguments, out var color))
            color = Color.ParseHex("#f09200");

        return seq.Glow(color);
    };

    [MemeCommandMapping("[#颜色]", "晕影")]
    public static Factory Vignette() => (seq, arguments, token) =>
    {
        if (!Color.TryParseHex(arguments, out var color))
            throw new AfterProcessError(nameof(Vignette), $"颜色{arguments}解析失败");

        return seq.Vignette(color);
    };

    [MemeCommandMapping("", "镜头模糊")]
    public static Factory BokehBlur() => (seq, arguments, token) => seq.BokehBlur();
    
    [MemeCommandMapping("", "高斯模糊")]
    public static Factory GaussianBlur() => (seq, arguments, token) => seq.GaussianBlur();

    private static bool TryParseNumberPair(string argument, [NotNullWhen(true)]out (float x, float y)? numberPair)
    {
        numberPair = null;
        var leftIdx = argument.IndexOf('(');
        if (leftIdx < 0) return false;
        
        var rightIdx = argument.IndexOf(')', leftIdx + 1);
        if (rightIdx < 0) return false;
        
        var pairStr = argument[(leftIdx + 1)..rightIdx];
        var pairStrArr = pairStr.Split(',', 2);

        var xStatus = float.TryParse(pairStrArr[0], out var x);
        var yStatus = float.TryParse(pairStrArr[1], out var y);
        numberPair = (x, y);
        
        return xStatus && yStatus;
    }
    
    [MemeCommandMapping("[迭代次数=10]", "径向模糊")]
    public static Factory RadialBlur()
    {
        return (seq, arguments, token) =>
        {
            if (!int.TryParse(arguments, out var iteration)) iteration = 10;

            var center = new Point(0.5, 0.5);
            if (TryParseNumberPair(arguments, out var numberPair))
                center = new Point(numberPair.Value.x, numberPair.Value.y);
            
            iteration = Math.Min(iteration, 20);
            return seq.OpenCv(mat => ValueTask.FromResult(mat.RadialBlur(center, iteration)),
                cancellationToken: token);
        };

    }

    [MemeCommandMapping("", "高斯锐化")]
    public static Factory GaussianSharpen() => (seq, arguments, token) => seq.GaussianSharpen();

    [MemeCommandMapping("", "黑白")]
    public static Factory BlackWhite() => (seq, arguments, token) => seq.BlackWhite();
    
    [MemeCommandMapping("", "反相")]
    public static Factory Invert() => (seq, arguments, token) => seq.Invert(1);
    
    [MemeCommandMapping("", "胶卷")]
    public static Factory Kodachrome() => (seq, arguments, token) => seq.Kodachrome();

    [MemeCommandMapping("", "拍立得")]
    public static Factory Polaroid() => (seq, arguments, token) => seq.Polaroid();

    [MemeCommandMapping("[大小] // 可选，默认5", "像素化")]
    public static Factory Pixelate() => (seq, arguments, token) =>
    {
        if (!int.TryParse(arguments, out var size)) size = 5;

        return seq.Pixelate(size);
    };

    [MemeCommandMapping("[数值]", "对比度")]
    public static Factory Contrast() => (seq, arguments, token) =>
    {
        if (!float.TryParse(arguments, out var amount)) 
            throw new AfterProcessError(nameof(Contrast), $"数值{arguments}解析失败");

        return seq.Contrast(amount);
    };

    [MemeCommandMapping("[数值]", "透明度")]
    public static Factory Opacity() => (seq, arguments, token) =>
    {
        if (!float.TryParse(arguments, out var amount)) 
            throw new AfterProcessError(nameof(Opacity), $"数值{arguments}解析失败");

        return seq.Opacity(amount);
    };

    [MemeCommandMapping("[数值]", "色相")]
    public static Factory Hue() => (seq, arguments, token) =>
    {
        if (!float.TryParse(arguments, out var amount)) 
            throw new AfterProcessError(nameof(Hue), $"数值{arguments}解析失败");

        return seq.Hue(amount);
    };

    [MemeCommandMapping("[数值]", "饱和度")]
    public static Factory Saturate() => (seq, arguments, token) =>
    {
        if (!float.TryParse(arguments, out var amount)) 
            throw new AfterProcessError(nameof(Saturate), $"数值{arguments}解析失败");

        return seq.Saturate(amount);
    };
    
    [MemeCommandMapping("[数值]", "亮度")]
    public static Factory Lightness() => (seq, arguments, token) =>
    {
        if (!float.TryParse(arguments, out var amount)) 
            throw new AfterProcessError(nameof(Lightness), $"数值{arguments}解析失败");

        return seq.Lightness(amount);
    };
    
    [MemeCommandMapping("[数值]", "明度")]
    public static Factory Brightness() => (seq, arguments, token) =>
    {
        if (!float.TryParse(arguments, out var amount)) 
            throw new AfterProcessError(nameof(Brightness), $"数值{arguments}解析失败");

        return seq.Brightness(amount);
    };

}
