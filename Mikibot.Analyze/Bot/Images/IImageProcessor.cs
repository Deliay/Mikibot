using Mirai.Net.Data.Messages;
using SixLabors.ImageSharp;

namespace Mikibot.Analyze.Bot.Images;

public interface IImageProcessor
{
    public ValueTask<bool> InitializeAsync(CancellationToken cancellationToken = default);
    
    public ValueTask<ImageProcessResult> ProcessImage(Image image,
        MessageChain messages, CancellationToken cancellationToken = default);
}