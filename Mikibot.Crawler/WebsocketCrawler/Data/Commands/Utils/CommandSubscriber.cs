using Mikibot.Crawler.WebsocketCrawler.Packet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mikibot.Crawler.WebsocketCrawler.Data.Commands.Utils
{
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
            if (!_handlers.ContainsKey(command))
            {
                _handlers.Add(command, new());
            }

            _handlers[command].Add((obj) => handler((T)obj));
        }

        public void Subscribe<T>(Func<T, Task> handler)
        {
            if (ICommandBase.IsKnown(typeof(T)))
            {
                Subscribe(ICommandBase.Mapping(typeof(T)), handler);
            }
        }
        public void Subscribe<T>(Action<T> handler) => Subscribe<T>(Wrap(handler));

        private static object SelectData(ICommandBase cmd)
        {
            if (cmd.Command is KnownCommands.DANMU_MSG)
            {
                return cmd.GetType().GetProperty("Info")?.GetValue(cmd)!;
            }

            return cmd.GetType().GetProperty("Data")?.GetValue(cmd)!;
        }

        public async Task Handle(ICommandBase? cmd)
        {
            if (cmd == null) return;
            if (_handlers.ContainsKey(cmd.Command))
            {
                await Task.WhenAll(_handlers[cmd.Command].Select(async (handler) => await handler(SelectData(cmd)!)));
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
}
