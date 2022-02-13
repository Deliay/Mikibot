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
        private static readonly Dictionary<Type, KnownCommands> _KnwonCommandType = new()
        {
            { typeof(ComboSend), KnownCommands.COMBO_SEND },
            { typeof(DanmuMsg), KnownCommands.DANMU_MSG },
            { typeof(GuardBuy), KnownCommands.GUARD_BUY },
            { typeof(InteractWord), KnownCommands.INTERACT_WORD },
            { typeof(OnlineRankCount), KnownCommands.ONLINE_RANK_COUNT },
            { typeof(OnlineRankV2), KnownCommands.ONLINE_RANK_V2 },
            { typeof(RoomRealTimeMessageUpdate), KnownCommands.ROOM_REAL_TIME_MESSAGE_UPDATE },
            { typeof(SendGift), KnownCommands.SEND_GIFT },
            { typeof(SuperChatMessage), KnownCommands.SUPER_CHAT_MESSAGE },
            { typeof(EntryEffect), KnownCommands.ENTRY_EFFECT },
        };

        private readonly Dictionary<KnownCommands, List<Func<object, ValueTask>>> _handlers = new();

        private static Func<T, ValueTask> Wrap<T>(Action<T> action)
        {
            return (t) =>
            {
                action(t);
                return ValueTask.CompletedTask;
            };
        }

        public void Subscribe<T>(KnownCommands command, Func<T, ValueTask> handler)
        {
            if (!_handlers.ContainsKey(command))
            {
                _handlers.Add(command, new());
            }

            _handlers[command].Add((obj) => handler((T)obj));
        }

        public void Subscribe<T>(Func<T, ValueTask> handler)
        {
            if (_KnwonCommandType.ContainsKey(typeof(T)))
            {
                Subscribe(_KnwonCommandType[typeof(T)], handler);
            }
        }
        public void Subscribe<T>(Action<T> handler) => Subscribe(Wrap(handler));

        public void Subscribe(Func<ComboSend, ValueTask> handler) => Subscribe(handler);
        public void Subscribe(Action<ComboSend> handler) => Subscribe(Wrap(handler));

        public void Subscribe(Func<DanmuMsg, ValueTask> handler) => Subscribe(handler);
        public void Subscribe(Action<DanmuMsg> handler) => Subscribe(Wrap(handler));

        public void Subscribe(Func<GuardBuy, ValueTask> handler) => Subscribe(handler);
        public void Subscribe(Action<GuardBuy> handler) => Subscribe(Wrap(handler));

        public void Subscribe(Func<InteractWord, ValueTask> handler) => Subscribe(handler);
        public void Subscribe(Action<InteractWord> handler) => Subscribe(Wrap(handler));

        public void Subscribe(Func<OnlineRankCount, ValueTask> handler) => Subscribe(handler);
        public void Subscribe(Action<OnlineRankCount> handler) => Subscribe(Wrap(handler));

        public void Subscribe(Func<OnlineRankV2, ValueTask> handler) => Subscribe(handler);
        public void Subscribe(Action<OnlineRankV2> handler) => Subscribe(Wrap(handler));

        public void Subscribe(Func<RoomRealTimeMessageUpdate, ValueTask> handler) => Subscribe(handler);
        public void Subscribe(Action<RoomRealTimeMessageUpdate> handler) => Subscribe(Wrap(handler));

        public void Subscribe(Func<SendGift, ValueTask> handler) => Subscribe(handler);
        public void Subscribe(Action<SendGift> handler) => Subscribe(Wrap(handler));

        public void Subscribe(Func<SuperChatMessage, ValueTask> handler) => Subscribe(handler);
        public void Subscribe(Action<SuperChatMessage> handler) => Subscribe(Wrap(handler));


        private static object SelectData(ICommandBase cmd)
        {
            if (cmd.Command is KnownCommands.DANMU_MSG)
            {
                return cmd.GetType().GetProperty("Info")?.GetValue(cmd)!;
            }

            return cmd.GetType().GetProperty("Data")?.GetValue(cmd)!;
        }

        public async ValueTask Handle(ICommandBase? cmd)
        {
            if (cmd == null) return;
            if (_handlers.ContainsKey(cmd.Command))
            {
                await Task.WhenAll(_handlers[cmd.Command].Select(async (handler) => await handler(SelectData(cmd)!)));
            }
        }

        public ValueTask Handle(Normal normal) => Handle(ICommandBase.Parse(normal.RawContent));

        public ValueTask Handle(IData data)
        {
            if (data.Type == PacketType.Normal)
            {
                return Handle((Normal)data);
            }
            return ValueTask.CompletedTask;
        }

        public void Dispose()
        {
            _handlers.Clear();
            GC.SuppressFinalize(this);
        }
    }
}
