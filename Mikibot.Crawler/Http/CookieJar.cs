using System.Security;

namespace Mikibot.Crawler.Http;

public record CookieJar(string Cookie)
{
    public CookieJar Of(string cookie) => new(cookie); 
}
