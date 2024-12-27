namespace Mikibot.Crawler.Http.Bilibili;

public class BiliLiveStreamCrawler(BiliLiveCrawler crawler) : HttpCrawler(crawler.Client)
{
    [Obsolete("This method already moved to class BiliLiveCrawler")]
    public Task<Stream> OpenLiveStream(string url, CancellationToken cancellationToken)
    {
        return crawler.OpenLiveStream(url, cancellationToken);
    }

    [Obsolete("This method already moved to class BiliLiveCrawler")]
    public Task OpenLiveStream(string url, Stream @out, CancellationToken cancellationToken)
    {
        return crawler.OpenLiveStream(url, @out, cancellationToken);
    }
}