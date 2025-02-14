using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Mirai.Net.Data.Messages;
using Mirai.Net.Data.Messages.Concretes;
using Mirai.Net.Data.Messages.Receivers;
using Mirai.Net.Data.Shared;
using Mirai.Net.Utils.Scaffolds;
using Satori.Client;
using Satori.Client.Extensions;
using Satori.Protocol.Elements;
using Satori.Protocol.Events;
using Satori.Protocol.Models;

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
            VoiceMessage { Path.Length: > 0 } image => new AudioElement() { Src = $"file://{image.Path}" },
            VoiceMessage { Base64.Length: > 0 } image => new AudioElement() { Src = $"blob://{image.Base64}" },
            SourceMessage { MessageId.Length : > 0 } source => new QuoteElement() {  Id = source.MessageId },
            AtMessage { Target.Length :> 0 } at => new AtElement() { Id = at.Target },
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
            AtElement at => new AtMessage() { Target = at.Id },
            _ => null
        };
    }
    private static IEnumerable<MessageBase> ConvertMessageToMirai(Message e)
    {
        var elements = ElementSerializer.Deserialize(e.Content);
        IEnumerable<MessageBase> source = [new SourceMessage() { MessageId = e.Id, }];
        return source
            .Concat(elements.Select(ConvertSingleMessageElementToSatori))
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

    private async Task StartAsync()
    {
        try
        {
            await Client!.StartAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Listen failed");
        }
    }
    private Login CurrentBot { get; set; }

    public string UserId => CurrentBot.SelfId!;

    public async ValueTask Run()
    {
        Client = new SatoriClient(EnvSatoriEndpoint, EnvSatoriToken);
        CurrentBot = await Client.GetLoginAsync();
        Bot = await Client.GetBotAsync();
        Bot.MessageCreated += BotOnMessageCreated;

        logger.LogInformation("准备启动机器人，账号 {}", CurrentBot.SelfId);
        _ = StartAsync();
    }


    private readonly HashSet<string> _messageIds = [];
    private void BotOnMessageCreated(object? sender, Event e)
    {
        if (e.Message is null) return;
        if (e is not { User: not null, Channel: not null }) return;
    
        // don't process the messages from bot self
        if (CurrentBot.SelfId == e.User.Id) return;
        
        if (!_messageIds.Add(e.Message.Id))
        {
            return;
        }

        var group = e.Channel is not null
            ? new Group()
            {
                Id = e.Channel.Id,
                Name = e.Channel.Name,
                Permission = Permissions.Member,
            } : null;
        var member = new Member()
        {
            Id = e.User.Id,
            Name = e.User.Name,
            Group = group,
        };
        var type = e.Channel is not null
            ? MessageReceivers.Group
            : MessageReceivers.Friend;
        
        var message = new MessageChain(ConvertMessageToMirai(e.Message));
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