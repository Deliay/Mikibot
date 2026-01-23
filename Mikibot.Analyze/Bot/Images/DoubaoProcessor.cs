using System.Net.Http.Json;
using System.Text;
using FFMpegCore;
using FFMpegCore.Pipes;
using MemeFactory.Core.Processing;
using MemeFactory.Core.Utilities;
using SixLabors.ImageSharp;

namespace Mikibot.Analyze.Bot.Images;

public static class DoubaoProcessor
{
    
    private record TextContent(string text, string type = "text");
    private record ImageUrl(string url);

    private record ImageContent(ImageUrl image_url, string role, string type = "image_url");

    private record Body(string model, List<object> content);

    private record InitialResult(string id);
    
    private static HttpClient Client = new HttpClient();

    private record VideoContent(string video_url);
    private record VideoError(string message);
    private record VideoUsage(int completion_tokens);
    private record VideoStatusResponse(string status, VideoContent content, VideoUsage usage, VideoError? error);

    private const string BaseGenerateUrl = "https://ark.cn-beijing.volces.com/api/v3/contents/generations/tasks";
    private const string BaseQueryUrl = "https://ark.cn-beijing.volces.com/api/v3/contents/generations/tasks";
    private const string Model = "doubao-seedance-1-0-pro-250528";

    private static async ValueTask<string> FrameToDataImage(Frame frame)
    {
        using var stream = new MemoryStream();
        await frame.Image.SaveAsPngAsync(stream);
        stream.Seek(0, SeekOrigin.Begin);

        return $"data:image/png;base64,{Convert.ToBase64String(stream.ToArray())}";
    }
    
    public static async IAsyncEnumerable<Frame> Process(this IAsyncEnumerable<Frame> frames, string prompt)
    {
        var frameList = await frames.ToListAsync();
        var first = await FrameToDataImage(frameList.First());
        var last = await FrameToDataImage(frameList.Last());
        var finalPrompt = $"{prompt} --ratio keep_ratio --resolution 480p --duration 5 --camerafixed true";
        var result = await Client.PostAsJsonAsync(BaseGenerateUrl, new Body(Model, [
            new TextContent(finalPrompt),
            new ImageContent(new ImageUrl(first), "first_frame"),
            new ImageContent(new ImageUrl(last), "last_frame")
        ]));

        result.EnsureSuccessStatusCode();
        var initialResult = await result.Content.ReadFromJsonAsync<InitialResult>()
                            ?? throw new NullReferenceException("豆包服务器返回了预期之外的数据1/3");

        var queryUrl = $"{BaseQueryUrl}/{initialResult.id}";
        var videoResult = await Client.GetFromJsonAsync<VideoStatusResponse>(queryUrl)
                ?? throw new NullReferenceException("豆包服务器返回了预期之外的数据2/3");
        while ("running" == videoResult.status)
        {
            await Task.Delay(1000);
            videoResult = await Client.GetFromJsonAsync<VideoStatusResponse>(queryUrl)
                          ?? throw new NullReferenceException("豆包服务器返回了预期之外的数据3/3");
        }

        if (videoResult.status != "succeeded")
        {
            foreach (var frame in frameList)
            {
                yield return frame;
            }

            throw new Exception("豆包出错了:" + videoResult.error!.message);
        }
        var video = await Client.GetStreamAsync(videoResult.content.video_url);
        using var gifMemoryStream = new MemoryStream();
        await FFMpegArguments
            .FromPipeInput(new StreamPipeSource(video))
            .OutputToPipe(new StreamPipeSink(gifMemoryStream), (args) =>
            {
                args.WithCustomArgument("-vf \"split[s1][s2];[s1]palettegen=max_colors=256[p];[s2][p]paletteuse=dither=bayer[f];");
                args.WithFramerate(24);
                args.ForceFormat("gif");
            })
            .ProcessAsynchronously(throwOnError: true);
            
        gifMemoryStream.Seek(0, SeekOrigin.Begin);
        using var image = await Image.LoadAsync(gifMemoryStream);
            
        await foreach (var extractFrame in image.ExtractFrames())
        {
            yield return extractFrame;
            yield break;
        }
    } 
}