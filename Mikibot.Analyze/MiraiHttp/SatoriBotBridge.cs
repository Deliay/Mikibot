using Microsoft.Extensions.Logging;
using Mirai.Net.Data.Messages;
using Mirai.Net.Data.Messages.Concretes;
using Mirai.Net.Data.Messages.Receivers;
using Mirai.Net.Data.Shared;
using Satori.Client;
using Satori.Client.Extensions;
using Satori.Protocol.Elements;
using Satori.Protocol.Events;

namespace Mikibot.Analyze.MiraiHttp;

public class SatoriBotBridge(ILogger<SatoriBotBridge> logger) : IDisposable, IMiraiService
{
    private static readonly string EnvSatoriEndpoint
        = Environment.GetEnvironmentVariable("ENV_SATORI_ENDPOINT") ?? "http://localhost:5500";

    private static readonly string EnvSatoriToken
        = Environment.GetEnvironmentVariable("ENV_SATORI_TOKEN") ?? "";

    private static Element? ConvertSingleMessageElementToSatori(MessageBase message)
    {
        return message switch
        {
            PlainMessage plain => new TextElement() { Text = plain.Text, },
            ImageMessage { Url.Length: > 0 } image => new ImageElement() { Src = image.Url, },
            ImageMessage { Base64.Length: > 0 } image => new ImageElement() { Src = $"blob://{image.Base64}" },
            ImageMessage { Path.Length: > 0 } image => new ImageElement() { Src = $"file://{image.Path}" },
            VoiceMessage { Url.Length: > 0 } image => new AudioElement() { Src = image.Url, },
            VoiceMessage { Base64.Length: > 0 } image => new AudioElement() { Src = $"blob://{image.Base64}" },
            VoiceMessage { Path.Length: > 0 } image => new AudioElement() { Src = $"file://{image.Path}" },
            _ => null
        };
    }

    private static IEnumerable<Element> ConvertMessageToSatori(IEnumerable<MessageBase> messageChain)
    {
        return messageChain
            .Select(ConvertSingleMessageElementToSatori)
            .OfType<Element>();
    }

    private static MessageBase? ConvertSingleMessageElementToSatori(Element message)
    {
        return message switch
        {
            TextElement plain => new PlainMessage() { Text = plain.Text, },
            _ => null
        };
    }
    private static IEnumerable<MessageBase> ConvertMessageToMirai(IEnumerable<Element> messages)
    {
        return messages
            .Select(ConvertSingleMessageElementToSatori)
            .OfType<MessageBase>();
    } 
    
    public async ValueTask SendMessageToGroup(Group group, CancellationToken token, params MessageBase[] messages)
    {
        ArgumentNullException.ThrowIfNull(Bot);
        
        await Bot.CreateMessageAsync(group.Id, ConvertMessageToSatori(messages));
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

    private SatoriClient? Client { get; set; }
    private SatoriBot? Bot { get; set; }
    public async ValueTask Run()
    {
        Client = new SatoriClient(EnvSatoriEndpoint, EnvSatoriToken);
        
        Bot = await Client.GetBotAsync();
        
        Bot.MessageCreated += BotOnMessageCreated;
        _ = Client.StartAsync();
    }

    private void BotOnMessageCreated(object? sender, Event e)
    {
        if (e.Message is not { User: not null, Channel: not null }) return;
        
        var group = e.Message.Channel is not null
            ? new Group()
            {
                Id = e.Message.Channel.Id,
                Name = e.Message.Channel.Name,
                Permission = Permissions.Member,
            } : null;
        var member = new Member()
        {
            Id = e.Message.User.Id,
            Name = e.Message.User.Name,
            Group = group,
        };
        var type = e.Channel is not null
            ? MessageReceivers.Group
            : MessageReceivers.Friend;
        
        var elements = ElementSerializer.Deserialize(e.Message.Content);
        var message = new MessageChain(ConvertMessageToMirai(elements));

        foreach (var (next, _) in _subscriber)
        {
            next(new GroupMessageReceiver()
            {
                Type = type,
                Sender = member,
                MessageChain = message,
            });
        }
    }

    public void Dispose()
    {
        Client?.Dispose();
    }
    
    public async ValueTask SendMessageToSomeGroup(HashSet<string> groupIds, CancellationToken token, params MessageBase[] messages)
    {
        ArgumentNullException.ThrowIfNull(Bot);
        
        var elements = ConvertMessageToSatori(messages).ToList();
        foreach (var groupId in groupIds)
        {
            await Bot.CreateMessageAsync(groupId, elements);
        }
    }
    
    public ValueTask SendMessageToAllGroup(CancellationToken token, params MessageBase[] messages)
    {
        throw new NotImplementedException();
    }
    public ValueTask SendMessageToSliceManGroup(CancellationToken token, params MessageBase[] messages)
    {
        throw new NotImplementedException();
    }
}