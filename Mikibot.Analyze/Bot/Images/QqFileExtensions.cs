using MemeFactory.Core.Processing;
using Mikibot.Analyze.MiraiHttp;
using Mikibot.Analyze.Utils;
using SixLabors.ImageSharp;

namespace Mikibot.Analyze.Bot.Images;

public static class QqFileExtensions
{
    public static async ValueTask<Image> ReadImageAsync(this IQqService qqService, string imageUrl, CancellationToken token)
    {
        await using var stream = await qqService.HttpClient.GetStreamAsync(imageUrl, token);
        return await Image.LoadAsync(stream, token);
    }
    
    
    public static async ValueTask<string> ToDataUri(this MemeResult result,
        CancellationToken cancellationToken = default)
    {
        await using var afterStream = new MemoryStream();
        await result.Image.SaveAsync(afterStream, result.Encoder, cancellationToken);
        afterStream.Position = 0;
        return DataUri.Build(result.MimeType, Convert.ToBase64String(afterStream.ToArray()));
    }
}
