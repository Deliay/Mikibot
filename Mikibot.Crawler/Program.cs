
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mikibot.Crawler.Http.Bilibili;
using Mikibot.Crawler.WebsocketCrawler.Client;
using Mikibot.Crawler.WebsocketCrawler.Data;
using Mikibot.Crawler.WebsocketCrawler.Data.Commands;
using Mikibot.Crawler.WebsocketCrawler.Package;
using Mikibot.Crawler.WebsocketCrawler.Packet;
using System.Text.Json;

var serviceBuilder = new ServiceCollection();
serviceBuilder.AddLogging(b => b.AddConsole());
serviceBuilder.AddSingleton<BiliLiveCrawler>();
serviceBuilder.AddTransient<WebsocketClient>();

using var services = serviceBuilder.BuildServiceProvider();
using var csc = new CancellationTokenSource();

var logger = services.GetRequiredService<ILogger<Program>>();
var wsClient = services.GetRequiredService<WebsocketClient>();

var original = BasePacket.Auth(114514, "1919810");
var bytes = original.ToByte();
var restored = BasePacket.ToPacket(bytes);

await wsClient.ConnectAsync(22748536, csc.Token);
Dictionary<string, List<object>> events = new();

await foreach (var @event in wsClient.Events(csc.Token))
{
    if (@event.Type == PacketType.Normal)
    {
        Normal normal = (Normal)@event;

        var command = JsonSerializer.Deserialize<CommandBase<object>>(normal.RawContent);

        if (!events.ContainsKey(command.Command))
        {
            events.Add(command.Command, new List<object>());
        }

        events[command.Command].Add(CommandBase<object>.Parse(normal.RawContent));

    }
    logger.LogInformation("event received: {}, serialize={}", @event.Type, JsonSerializer.Serialize(@event, @event.GetType()));
}