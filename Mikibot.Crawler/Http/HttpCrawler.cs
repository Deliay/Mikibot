using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Mikibot.Crawler.Http
{
    public class HttpCrawler : IDisposable
    {
        protected readonly HttpClient client = new();

        public HttpCrawler()
        {
            AddHeader("Origin", "https://live.bilibili.com/");
            AddHeader("Referer", "https://live.bilibili.com/");
        }

        protected void AddHeader(string key, string value)
        {
            client.DefaultRequestHeaders.Add(key, value);
        }

        public void SetCookie(string cookie)
        {
            AddHeader("cookie", cookie);
        }

        protected JsonSerializerOptions JsonParseOptions { get; set; } = new JsonSerializerOptions()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        protected async ValueTask<T?> GetAsync<T>(string url, CancellationToken token)
        {
            var raw = await client.GetAsync(url, token);
            var stream = raw.Content.ReadAsStream(token);
            try
            {
                return await JsonSerializer.DeserializeAsync<T>(stream, JsonParseOptions, token);
            }
            catch (JsonException jsonException)
            {
                var failedStr = await raw.Content.ReadAsStringAsync(token);
                Console.WriteLine(failedStr);
                throw jsonException;
            }
        }

        protected async ValueTask<T?> PostFormAsync<T>(string url, FormUrlEncodedContent ctx, CancellationToken token)
        {
            var raw = await client.PostAsync(url, ctx, token);
            var stream = raw.Content.ReadAsStream(token);
            return await JsonSerializer.DeserializeAsync<T>(stream, JsonParseOptions, token);
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            client.Dispose();
        }
    }
}
