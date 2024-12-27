using System.Text.Json.Serialization;

namespace Mikibot.Crawler.Http.Bilibili.Model;

public struct BilibiliApiResponse<T>
{
    [JsonPropertyName("code")]
    public int Code { get; set; }
        
    [JsonPropertyName("data")]
    public T Data { get; set; }
        
    [JsonPropertyName("message")]
    public string Message { get; set; }

    public void AssertCode()
    {
        if (Code != 0)
        {
            throw new InvalidOperationException();
        }
    }
}