using System.Collections.Concurrent;
using ExtendedNumerics;
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

    private static MessageElement ConvertForwardMessage(ForwardMessage forward)
    {
        var root = new MessageElement()
        {
            Forward = true,
        };
        foreach (var node in forward.NodeList)
        {
            var child = new MessageElement();
            root.ChildElements.Add(child);
            child.ChildElements.Add(new AuthorElement()
            {
                UserId = node.SenderId,
                Nickname = node.SenderName,
            });
            foreach (var element in node.MessageChain
                         .Select(ConvertSingleMessageElementToSatori))
            {
                if (element is not null) child.ChildElements.Add(element);
            }
            
        }
        return root;
    }
    
    private static Element? ConvertSingleMessageElementToSatori(MessageBase message)
    {
        return message switch
        {
            PlainMessage plain => new TextElement() { Text = plain.Text, },
            ImageMessage { Url.Length: > 0 } image => new ImageElement() { Src = image.Url, Cache = true, Attributes =
            {
                { "title", $"{Guid.NewGuid().ToString()}.jpg" },
            } },
            ImageMessage { Base64.Length: > 0 } image => new ImageElement() { Src = image.Base64 },
            ImageMessage { Path.Length: > 0 } image => new ImageElement() { Src = image.Path },
            SourceMessage { MessageId.Length : > 0 } source => new QuoteElement() {  Id = source.MessageId },
            AtMessage { Target.Length :> 0 } at => new AtElement() { Id = at.Target },
            QuoteMessage quote => new QuoteElement() { Id = quote.MessageId },
            ForwardMessage forward => ConvertForwardMessage(forward),
            _ => null
        };
    }

    private static IEnumerable<Element> ConvertMessageToSatori(IEnumerable<MessageBase> messageChain)
    {
        return messageChain
            .Select(ConvertSingleMessageElementToSatori)
            .OfType<Element>();
    }

    private MessageBase? ConvertSingleMessageElementToSatori(Element message)
    {
        if (message is ResourceElement resource)
        {
            logger.LogInformation("Convert local resource address to remote: {}", resource.Src);
            if (resource.Src.Contains("127.0.0.1")) resource.Src = new Uri(resource.Src).AbsolutePath;
        }

        return message switch
        {
            TextElement plain => new PlainMessage() { Text = plain.Text, },
            AtElement at => new AtMessage() { Target = at.Id },
            ImageElement image => new ImageMessage() { Url = image.Src },
            QuoteElement quote => new QuoteMessage()
            {
                MessageId = quote.Id,
                Origin = quote.ChildElements.Select(ConvertSingleMessageElementToSatori).ToList(),
            },
            _ => null
        };
    }
    private IEnumerable<MessageBase> ConvertMessageToMirai(Message e)
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
        
        await Bot.CreateMessageAsync(group.Id, ConvertMessageToSatori(messages), token);
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

    public HttpClient HttpClient { get; } = new() { BaseAddress = new Uri(EnvSatoriEndpoint) };

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

        if (group?.Id == "244534796")
        {
            logger.LogInformation("Debug raw message: {}", e.Message.Content);
        }
        
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
    
    public async ValueTask<Dictionary<string, string>> SendMessageToSomeGroup(HashSet<string> groupIds, CancellationToken token, params MessageBase[] messages)
    {
        ArgumentNullException.ThrowIfNull(Bot);
        
        var elements = ConvertMessageToSatori(messages).ToList();

        Dictionary<string, string> resultSet = [];
        foreach (var groupId in groupIds)
        {
            var result = await Bot.CreateMessageAsync(groupId, elements, token);
            
            resultSet.Add(groupId, result.First().Id);
        }

        return resultSet;
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