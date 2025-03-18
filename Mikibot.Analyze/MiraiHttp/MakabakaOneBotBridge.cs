using System.Net.Security;
using System.Runtime.InteropServices;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Makabaka;
using Makabaka.Events;
using Makabaka.Messages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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

    private static HttpClient MakeInsecureSslSupportHttpClient()
    {
        var socketsHttpHandler = new SocketsHttpHandler();
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            socketsHttpHandler.SslOptions.CipherSuitesPolicy = new CipherSuitesPolicy(
            [
                TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_128_GCM_SHA256,
                TlsCipherSuite.TLS_RSA_WITH_AES_256_GCM_SHA384,
                TlsCipherSuite.TLS_RSA_WITH_AES_128_GCM_SHA256,
            ]);
        }
        return new HttpClient(socketsHttpHandler);
    }

    public HttpClient HttpClient { get; } = MakeInsecureSslSupportHttpClient();
    
    private Task? _botRunTask;
    public ValueTask Run(CancellationToken cancellationToken = default)
    {
        IServiceCollection services = new ServiceCollection();
        services.AddMakabaka();
        var config = new ConfigurationBuilder()
            .AddCommandLine(Environment.GetCommandLineArgs())
            .AddEnvironmentVariables()
            .Build();
        services.AddSingleton(config);
        services.AddSingleton<IConfiguration>(config);
        
        _makabakaScope = scope.BeginLifetimeScope(c =>
        {
            c.Register(ctx => new AutofacServiceProvider(ctx.Resolve<ILifetimeScope>())).As<IServiceProvider>();
            c.Populate(services);
        });
        var bot = _makabakaScope.Resolve<IHostedService>();
        
        _botContext = _makabakaScope.Resolve<IBotContext>();
        _botContext.OnGroupMessage += BotContextOnOnGroupMessage;
        
        _botRunTask = bot.StartAsync(cancellationToken);
        
        return ValueTask.CompletedTask;
    }

    private Segment? Trace(MessageBase message)
    {
        logger.LogInformation("Unsupported message: {}", message.ToString());
        return null;
    }
    
    private Segment? Convert(MessageBase message)
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
            _ => Trace(message),
        };
    }

    private Message Convert(IEnumerable<MessageBase> message)
    {
        var msg = new Message();
        msg.AddRange(message.Select(Convert).Where(m => m is not null)!);
        
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
                    yield return new ImageMessage()
                    {
                        Url = imageSegment.Data.File,
                    };
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

        var msgChain = new MessageChain(await Convert(e.Message).ToListAsync())
        {
            new SourceMessage() { MessageId = e.MessageId.ToString() },
        };
        return new GroupMessageReceiver()
        {
            Sender = new Member()
            {
                Id = senderId,
                Group = new Group() { Id = groupId},
                Name = e.Sender?.Nickname,
            },
            MessageChain = msgChain,
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