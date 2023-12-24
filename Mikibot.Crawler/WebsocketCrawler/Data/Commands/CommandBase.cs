using Mikibot.Crawler.WebsocketCrawler.Data.Commands.KnownCommand;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Mikibot.Crawler.WebsocketCrawler.Data.Commands
{
    public interface ICommandBase
    {
        public KnownCommands Command { get; set; }

        private static readonly Dictionary<KnownCommands, Type> CommandTypeMapping = new()
        {
            { KnownCommands.DANMU_MSG, typeof(DanmuMsg) },
            { KnownCommands.SEND_GIFT, typeof(SendGift) },
            { KnownCommands.GUARD_BUY, typeof(GuardBuy) },
            { KnownCommands.ROOM_REAL_TIME_MESSAGE_UPDATE, typeof(RoomRealTimeMessageUpdate) },
            { KnownCommands.SUPER_CHAT_MESSAGE, typeof(SuperChatMessage) },
            { KnownCommands.COMBO_SEND, typeof(ComboSend) },
            { KnownCommands.INTERACT_WORD, typeof(InteractWord) },
            { KnownCommands.ONLINE_RANK_COUNT, typeof(OnlineRankCount) },
            { KnownCommands.ONLINE_RANK_V2, typeof(OnlineRankV2) },
            { KnownCommands.ENTRY_EFFECT, typeof(EntryEffect) },
            { KnownCommands.ANCHOR_LOT_START, typeof(AnchorLotStart) },
            { KnownCommands.ANCHOR_LOT_AWARD, typeof(AnchorLotAward) },
            { KnownCommands.POPULARITY_RED_POCKET_START, typeof(PopularityRedPocketStart) },
            { KnownCommands.HOT_RANK_SETTLEMENT_V2, typeof(HotRankSettlementV2) },
        };
        private static readonly Dictionary<Type, KnownCommands> KnownCommandMapping =
            CommandTypeMapping.ToDictionary(p => p.Value, p => p.Key);

        public static Type Mapping(KnownCommands command) => CommandTypeMapping[command];
        public static KnownCommands Mapping(Type type) => KnownCommandMapping[type];
        public static bool IsKnown(KnownCommands command) => CommandTypeMapping.ContainsKey(command);
        public static bool IsKnown(Type type) => KnownCommandMapping.ContainsKey(type);

        private static readonly Type PartialCommandBase = typeof(CommandBase<>);
        private static readonly Dictionary<KnownCommands, Type> CommandMapping =
            CommandTypeMapping.ToDictionary(p => p.Key, p => PartialCommandBase.MakeGenericType(p.Value));

        private static readonly JsonSerializerOptions JsonSerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        public static ICommandBase? Parse(string raw)
        {
            try
            {
                var cmd = JsonSerializer.Deserialize<CommandBase<JsonElement>>(raw, JsonSerializerOptions);
#if DEBUG
                Debug.WriteLine($"cmd: {cmd.Command}, data: {cmd.Data}, info {cmd.Info}");
#endif
                if (CommandMapping.ContainsKey(cmd.Command))
                {
                    var type = CommandMapping[cmd.Command];
                    var underlying = type.GenericTypeArguments[0];
                    var instance = Activator.CreateInstance(type)!;

                    instance.GetType().GetProperty("Command")?.SetValue(instance, cmd.Command);

                    if (cmd.Info.ValueKind != JsonValueKind.Undefined)
                        instance.GetType().GetProperty("Info")?.SetValue(instance, cmd.Info.Deserialize(underlying, JsonSerializerOptions));

                    if (cmd.Data.ValueKind != JsonValueKind.Undefined)
                        instance.GetType().GetProperty("Data")?.SetValue(instance, cmd.Data.Deserialize(underlying, JsonSerializerOptions));

                    return (ICommandBase)instance;
                }
                return cmd;
            }
            catch (Exception ex)
            {
#if DEBUG
                Debug.WriteLine("{0} error {1}", raw, ex.ToString());
#endif
                return null!;
            }
        }
    }

    public struct CommandBase<T> : ICommandBase
    {
        [JsonPropertyName("cmd")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public KnownCommands Command { get; set; }
        [JsonPropertyName("data")]
        public T Data { get; set; }
        [JsonPropertyName("info")]
        public T Info { get; set; }
    }
}
