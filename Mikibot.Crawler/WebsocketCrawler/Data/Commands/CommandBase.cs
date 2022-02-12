using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Mikibot.Crawler.WebsocketCrawler.Data.Commands
{
    public struct CommandBase<T>
    {
        [JsonPropertyName("cmd")]
        public string Command { get; set; }
        [JsonPropertyName("data")]
        public T Data { get; set; }
        [JsonPropertyName("info")]
        public T Info { get; set; }

        private static readonly Type PartialCommandType = typeof(CommandBase<>);
        private static readonly Dictionary<string, Type> CommandMapping = new()
        {
            { "DANMU_MSG", PartialCommandType.MakeGenericType(typeof(DanmuMsg)) },
            { "SEND_GIFT", PartialCommandType.MakeGenericType(typeof(SendGift)) },
            { "GUARD_BUY", PartialCommandType.MakeGenericType(typeof(GuardBuy)) },
            { "ROOM_REAL_TIME_MESSAGE_UPDATE", PartialCommandType.MakeGenericType(typeof(RoomRealTimeMessageUpdate)) },
            { "SUPER_CHAT_MESSAGE", PartialCommandType.MakeGenericType(typeof(SuperChatMessage)) },
            { "COMBO_SEND", PartialCommandType.MakeGenericType(typeof(ComboSend)) },
            { "INTERACT_WORD", PartialCommandType.MakeGenericType(typeof(InteractWord)) },
            { "ONLINE_RANK_COUNT", PartialCommandType.MakeGenericType(typeof(OnlineRankCount)) },
            { "ONLINE_RANK_V2", PartialCommandType.MakeGenericType(typeof(OnlineRankV2)) },
        };

        private static readonly JsonSerializerOptions JsonSerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        public static object Parse(string raw)
        {
            var cmd = JsonSerializer.Deserialize<CommandBase<JsonElement>>(raw, JsonSerializerOptions);
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

                return instance;
            }

            return cmd;
        }
    }
}
