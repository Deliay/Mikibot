using System.Runtime.CompilerServices;
using MemeFactory.Core.Processing;
using OpenCvSharp;
using SixLabors.ImageSharp;

namespace Mikibot.Analyze.Bot.Images.OpenCv;

public static class OpenCvFrameProcessExtensions
{
    public static async IAsyncEnumerable<Frame> OpenCv(this IAsyncEnumerable<Frame> frames,
        Func<Mat, ValueTask<Mat>> transform,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var frame in frames.WithCancellation(cancellationToken)) using (frame)
        {
            using var ms = new MemoryStream(); 
            await frame.Image.SaveAsPngAsync(ms, cancellationToken);
            ms.Position = 0;
            using var mat = Cv2.ImDecode(ms.GetBuffer(), ImreadModes.Color);
            using var newMat = await transform(mat);
            Cv2.ImEncode(".png", newMat, out var buf);
            yield return frame with { Image = Image.Load(buf) };
        }
    }
}