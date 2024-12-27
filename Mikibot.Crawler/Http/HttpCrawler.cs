using System.Text.Json;

namespace Mikibot.Crawler.Http;

public class HttpCrawler : IDisposable
{
    protected readonly HttpClient client;

    public HttpCrawler() : this(new HttpClient())
    {
    }
        
    public HttpCrawler(HttpClient client)
    {
        this.client = client;
        AddHeader("Origin", "https://live.bilibili.com/");
        AddHeader("Referer", "https://live.bilibili.com/");
    }

    protected void AddHeader(string key, string value)
    {
        client.DefaultRequestHeaders.Remove(key);
        client.DefaultRequestHeaders.Add(key, value);
    }

    public void SetCookie(string cookie)
    {
        AddHeader("Cookie", cookie);
    }

    protected JsonSerializerOptions JsonParseOptions { get; set; } = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    protected virtual ValueTask BeforeRequestAsync(CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }
        
    protected async ValueTask<T?> GetAsync<T>(string url, CancellationToken token)
    {
        await BeforeRequestAsync(token);
            
        var raw = await client.GetAsync(url, token);
        var stream = await raw.Content.ReadAsStreamAsync(token);
        try
        {
            return await JsonSerializer.DeserializeAsync<T>(stream, JsonParseOptions, token);
        }
        catch (JsonException)
        {
            var failedStr = await raw.Content.ReadAsStringAsync(token);
            Console.WriteLine(failedStr);
            throw;
        }
    }

    protected async ValueTask<T?> PostFormAsync<T>(string url, FormUrlEncodedContent ctx, CancellationToken token)
    {
        await BeforeRequestAsync(token);
            
        var raw = await client.PostAsync(url, ctx, token);
        var stream = await raw.Content.ReadAsStreamAsync(token);
        return await JsonSerializer.DeserializeAsync<T>(stream, JsonParseOptions, token);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        client.Dispose();
    }
}