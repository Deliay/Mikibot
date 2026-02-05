namespace Mikibot.Crawler.WebsocketCrawler.Data.Commands.KnownCommand;

public struct OnlineRankCount : IKnownCommand
{
    public int Count { get; set; }
}