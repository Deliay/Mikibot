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

        private readonly Dictionary<KnownCommands, List<Func<object, Task>>> _handlers = new();

        private static Func<T, Task> Wrap<T>(Action<T> action)
        {
            return (t) =>
            {
                action(t);
                return Task.CompletedTask;
            };
        }

        public void Subscribe<T>(KnownCommands command, Func<T, Task> handler)
        {
            if (!_handlers.ContainsKey(command))
            {
                _handlers.Add(command, new());
            }

            _handlers[command].Add((obj) => handler((T)obj));
        }

        public void Subscribe<T>(Func<T, Task> handler)
        {
            if (_KnwonCommandType.ContainsKey(typeof(T)))
            {
                Subscribe(_KnwonCommandType[typeof(T)], handler);
            }
        }
        public void Subscribe<T>(Action<T> handler) => Subscribe<T>(Wrap(handler));

        public void Subscribe(Func<ComboSend, Task> handler) => Subscribe<ComboSend>(handler);
        public void Subscribe(Action<ComboSend> handler) => Subscribe<ComboSend>(Wrap(handler));

        public void Subscribe(Func<DanmuMsg, Task> handler) => Subscribe<DanmuMsg>(handler);
        public void Subscribe(Action<DanmuMsg> handler) => Subscribe<DanmuMsg>(Wrap(handler));

        public void Subscribe(Func<EntryEffect, Task> handler) => Subscribe<EntryEffect>(handler);
        public void Subscribe(Action<EntryEffect> handler) => Subscribe<EntryEffect>(Wrap(handler));

        public void Subscribe(Func<GuardBuy, Task> handler) => Subscribe<GuardBuy>(handler);
        public void Subscribe(Action<GuardBuy> handler) => Subscribe<GuardBuy>(Wrap(handler));

        public void Subscribe(Func<InteractWord, Task> handler) => Subscribe<InteractWord>(handler);
        public void Subscribe(Action<InteractWord> handler) => Subscribe<InteractWord>(Wrap(handler));

        public void Subscribe(Func<OnlineRankCount, Task> handler) => Subscribe<OnlineRankCount>(handler);
        public void Subscribe(Action<OnlineRankCount> handler) => Subscribe<OnlineRankCount>(Wrap(handler));

        public void Subscribe(Func<OnlineRankV2, Task> handler) => Subscribe<OnlineRankV2>(handler);
        public void Subscribe(Action<OnlineRankV2> handler) => Subscribe<OnlineRankV2>(Wrap(handler));

        public void Subscribe(Func<RoomRealTimeMessageUpdate, Task> handler) => Subscribe<RoomRealTimeMessageUpdate>(handler);
        public void Subscribe(Action<RoomRealTimeMessageUpdate> handler) => Subscribe<RoomRealTimeMessageUpdate>(Wrap(handler));

        public void Subscribe(Func<SendGift, Task> handler) => Subscribe<SendGift>(handler);
        public void Subscribe(Action<SendGift> handler) => Subscribe<SendGift>(Wrap(handler));

        public void Subscribe(Func<SuperChatMessage, Task> handler) => Subscribe<SuperChatMessage>(handler);
        public void Subscribe(Action<SuperChatMessage> handler) => Subscribe<SuperChatMessage>(Wrap(handler));


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
