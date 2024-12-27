using Mikibot.Crawler.WebsocketCrawler.Data.Commands.KnownCommand;
using Mikibot.Crawler.WebsocketCrawler.Packet;

namespace Mikibot.Crawler.WebsocketCrawler.Data.Commands.Utils;

public class CommandSubscriber : IDisposable
{
    private readonly Dictionary<KnownCommands, List<Func<object, Task>>> _handlers = new();

    private static Func<T, Task> Wrap<T>(Action<T> action)
    {
        return (t) =>
        {
            action(t);
            return Task.CompletedTask;
        };
    }

    private void Subscribe<T>(KnownCommands command, Func<T, Task> handler)
    {
        if (!CommandBaseHelper.IsKnown(command))
        {
            throw new InvalidOperationException($"Can't map type {typeof(T)} to command enum");
        }
        
        if (!_handlers.ContainsKey(command))
        {
            _handlers.Add(command, []);
        }

        _handlers[command].Add((obj) => handler((T)obj));
    }

    public void Subscribe<T>(Func<T, Task> handler)
    {
        if (CommandBaseHelper.IsKnown(typeof(T)))
        {
            Subscribe(CommandBaseHelper.Mapping(typeof(T)), handler);
        }
        else
        {
            throw new InvalidOperationException($"Can't map type {typeof(T)} to command enum");
        }
    }
    
    public void Subscribe<T>(Action<T> handler) => Subscribe(Wrap(handler));

    private static object SelectData(ICommandBase cmd)
    {
        if (cmd is CommandBase<DanmuMsg> danmuMsg)
        {
            return danmuMsg.Info;
        }

        var (infoGetter, dataGetter) = CommandBaseHelper.GetCommandPropertyGetters(cmd.GetType());
        
        return dataGetter(cmd) ?? infoGetter(cmd)!;
    }

    public async Task Handle(ICommandBase? cmd)
    {
        if (cmd == null) return;
        if (_handlers.TryGetValue(cmd.Command, out var cmdHandler))
        {
            await Task.WhenAll(cmdHandler.Select(async (handler) => await handler(SelectData(cmd))));
        }
    }

    public Task Handle(Normal normal) => Handle(ICommandBase.Parse(normal.RawContent));

    public Task Handle(IData data)
    {
        if (data.Type == PacketType.Normal)
        {
            return Handle((Normal)data);
        }
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _handlers.Clear();
        GC.SuppressFinalize(this);
    }
}