using System.Text.Json;
using Mikibot.Crawler.Http.Bilibili.Model;

namespace Mikibot.Crawler.Http.Bilibili;

public class BiliUserCrawler(BilibiliAccount account, HttpClient client, CookieJar? cookieJar = null) : HttpCrawler(client, cookieJar)
{
    protected override async ValueTask BeforeRequestAsync(CancellationToken cancellationToken)
    {
        await base.BeforeRequestAsync(cancellationToken);
        AddHeader("User-Agent", "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_11_1) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/52.0.2743.116 Safari/537.36");
        AddHeader("Origin", "https://space.bilibili.com");
        AddHeader("X-Requested-With", "XMLHttpRequest");
        AddHeader("AlexaToolbar-ALX_NS_PH", "AlexaToolbar/alx-4.0");
        
    }

    public async ValueTask<PersonalInfo> GetPersonalInfo(long uid, CancellationToken token = default)
    {
        AddHeader("Referer", $"https://space.bilibili.com/{uid}?from=search&seid={Random.Shared.NextInt64(10000, 50000)}");
        var query = account.Sign([
            new("mid", $"{uid}"),
            new("type", "0"),
            new("web_location", "444.8")
        ]);
        var result = await GetAsync<BilibiliApiResponse<PersonalInfo>>($"https://api.bilibili.com/x/space/acc/info?{query}", token);
        result.AssertCode();

        return result.Data;
    }
    
    public async ValueTask<JsonElement> GetRawPersonalInfo(long uid, CancellationToken token = default)
    {
        AddHeader("Referer", $"https://space.bilibili.com/{uid}?from=search&seid={Random.Shared.NextInt64(10000, 50000)}");
        var query = account.Sign([
            new("mid", $"{uid}"),
            new("type", "0"),
            new("web_location", "444.8")
        ]);
        await Task.Delay(TimeSpan.FromSeconds(3), token);
        var result = await GetAsync<BilibiliApiResponse<JsonElement>>($"https://api.bilibili.com/x/space/acc/info?{query}", token);
        result.AssertCode();

        return result.Data;
    }

}