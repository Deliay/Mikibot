using Microsoft.Extensions.DependencyInjection;
using Mikibot.Crawler.Http.Bilibili;

namespace Mikibot.Crawler;

public static class CrawlerExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddBilibiliCrawlers(bool addHttpClient = true)
        {
            if (addHttpClient) services.AddSingleton<HttpClient>();
            services.AddSingleton<BiliLiveCrawler>();
            services.AddSingleton<BiliBasicInfoCrawler>();
            services.AddSingleton<BilibiliAccount>();

            return services;
        }
    }
}