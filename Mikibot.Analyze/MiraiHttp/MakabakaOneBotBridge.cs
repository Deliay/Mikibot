using System.Text.Json;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Makabaka;
using Makabaka.Events;
using Makabaka.Messages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mikibot.Analyze.Utils;
using Mirai.Net.Data.Messages;
using Mirai.Net.Data.Messages.Concretes;
using Mirai.Net.Data.Messages.Receivers;
using Mirai.Net.Data.Shared;

namespace Mikibot.Analyze.MiraiHttp;

public class MakabakaOneBotBridge(ILifetimeScope scope, ILogger<MakabakaOneBotBridge> logger) : IQqService, IDisposable
{
    private ILifetimeScope _makabakaScope = null!;
    private IBotContext _botContext = null!;
    public HttpClient HttpClient { get; } = new();
    public ValueTask Run()
    {
        IServiceCollection services = new ServiceCollection();
        services.AddMakabaka();
        var config = new ConfigurationBuilder()
            .AddCommandLine(Environment.GetCommandLineArgs())
            .AddEnvironmentVariables()
            .Build();
        services.AddSingleton(config);
        services.AddSingleton<IConfiguration>(config);
        services.AddSingleton<IServiceProvider>(sp => new AutofacServiceProvider(sp.GetRequiredService<ILifetimeScope>()));
        
        _makabakaScope = scope.BeginLifetimeScope(c => c.Populate(services));
        _botContext = _makabakaScope.Resolve<IBotContext>();
        _botContext.OnGroupMessage += BotContextOnOnGroupMessage;
        return ValueTask.CompletedTask;
    }

    private Segment Convert(MessageBase message)
    {
        return message switch
        {
            PlainMessage plainMessage => new TextSegment(plainMessage.Text),
            ImageMessage { Url.Length: > 0 } imageMessage => new ImageSegment(imageMessage.Url),
            ImageMessage { Base64.Length: > 0 } imageMessage => new ImageSegment("base64://" + DataUri.GetData(imageMessage.Base64)),
            ImageMessage { Path.Length: > 0 } imageMessage => new ImageSegment(new UriBuilder()
            {
                Host = "",
                Scheme = Uri.UriSchemeFile,
                Path = imageMessage.Path,
            }.Uri.ToString()),
            AtMessage atMessage => new AtSegment(atMessage.Target),
            QuoteMessage quoteMessage => new ReplySegment(quoteMessage.MessageId),
        };
    }

    private Message Convert(IEnumerable<MessageBase> message)
    {
        var msg = new Message();
        msg.AddRange(message.Select(Convert));
        
        return msg;
    }
    
    private async IAsyncEnumerable<MessageBase> Convert(Message message, bool omitReply = false)
    {
        foreach (var segment in message)
        {
            switch (segment)
            {
                case TextSegment textSegment:
                    yield return new PlainMessage(textSegment.Data.Text);
                    break;
                case AtSegment atSegment:
                    yield return new AtMessage(atSegment.Data.QQ);
                    break;
                case ImageSegment imageSegment:
                    Console.WriteLine(JsonSerializer.Serialize(imageSegment.Data));
                    break;
                case ReplySegment replySegment:
                {
                    if (omitReply) break;

                    var msg = await _botContext.GetMessageAsync(long.Parse(replySegment.Data.Id));
                    msg.EnsureSuccess();
                    yield return new QuoteMessage()
                    {
                        MessageId = replySegment.Data.Id,
                        SenderId = msg.Data!.Sender.UserId.ToString(),
                        Origin = await Convert(msg.Data.Message, omitReply: true).ToListAsync()
                    };
                    break;
                }
            }
        }
    }
    
    private async ValueTask<GroupMessageReceiver> Convert(GroupMessageEventArgs e)
    {
        var senderId = e.UserId.ToString();
        var groupId = e.GroupId.ToString();

        return new GroupMessageReceiver()
        {
            Sender = new Member()
            {
                Id = senderId,
                Group = new Group() { Id = groupId},
                Name = e.Sender?.Nickname,
            },
            MessageChain = new MessageChain(await Convert(e.Message).ToListAsync()),
            Type = MessageReceivers.Group
        };
    }
    
    private async Task BotContextOnOnGroupMessage(object sender, GroupMessageEventArgs e)
    {
        var miraiMessage = await Convert(e);

        foreach (var (subscriber, _) in _subscriber)
        {
            subscriber(miraiMessage);
        }
    }

    private readonly Dictionary<Action<GroupMessageReceiver>, CancellationTokenRegistration> _subscriber = [];
    public void SubscribeMessage(Action<GroupMessageReceiver> next, CancellationToken token)
    {
        logger.LogInformation("注册消费消息: {}", next);
        _subscriber.Add(next, token.Register(() =>
        {
            _subscriber.TryGetValue(next, out var reg);
            using var regDispose = reg;
            _subscriber.Remove(next);
        }));
    }

    public string UserId => _botContext.SelfId.ToString();
    
    public async ValueTask SendMessageToGroup(Group group, CancellationToken token, params MessageBase[] messages)
    {
        await SendMessageToSomeGroup([group.Id], token, messages);
    }

    public async ValueTask<Dictionary<string, string>> SendMessageToSomeGroup(HashSet<string> groupIds, CancellationToken token, params MessageBase[] messages)
    {
        Dictionary<string, string> resultDict = [];
        foreach (var groupId in groupIds.Select(ulong.Parse))
        {
            var res = await _botContext.SendGroupMessageAsync(groupId, Convert(messages), token);
            if (res.Data is { MessageId: > 0 })
            {
                resultDict.Add(groupId.ToString(), res.Data!.MessageId.ToString());
            }
        }

        return resultDict;
    }

    public void Dispose()
    {
        _makabakaScope.Dispose();
        HttpClient.Dispose();
    }
}