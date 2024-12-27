using Mikibot.Crawler.WebsocketCrawler.Data.Commands.KnownCommand;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mikibot.Crawler.WebsocketCrawler.Data.Commands;

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

    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    

    private static readonly MethodInfo RawMethod = ((Delegate)Convert<int>).Method.GetGenericMethodDefinition();
    private delegate ICommandBase ConvertDelegate(CommandBase<JsonElement> json, JsonSerializerOptions options = null!);
    private static ICommandBase Convert<T>(CommandBase<JsonElement> json, JsonSerializerOptions options = null!)
    {
        var cmd = new CommandBase<T>()
        {
            Command = json.Command,
        };
        if (json.Data.ValueKind != JsonValueKind.Undefined) cmd.Data = json.Data.Deserialize<T>(options)!;
        if (json.Info.ValueKind != JsonValueKind.Undefined) cmd.Info = json.Info.Deserialize<T>(options)!;

        return cmd;
    }
    
    private static readonly Dictionary<Type, ConvertDelegate> CommandFactoryConverter = [];
    private static ConvertDelegate GetCommandConverter(Type underlyingType)
    {
        if (CommandFactoryConverter.TryGetValue(underlyingType, out var converter)) return converter;
        
        // (json, options)
        var argJson = Expression.Parameter(typeof(CommandBase<JsonElement>));
        var argJsonOptions = Expression.Parameter(typeof(JsonSerializerOptions));

        var method = RawMethod.MakeGenericMethod(underlyingType);
        // Convert(json, options)
        var convert = Expression.Call(method, argJson, argJsonOptions);
        // (json, options) => Convert(json, options)
        var lambda = Expression.Lambda<ConvertDelegate>(convert, argJson, argJsonOptions);
        
        CommandFactoryConverter.Add(underlyingType, converter = lambda.Compile());

        return converter;
    }

    public static ICommandBase? Parse(string raw)
    {
        try
        {
            var rawCmd = JsonSerializer.Deserialize<JsonElement>(raw, JsonSerializerOptions);
            if (!rawCmd.TryGetProperty("cmd", out var cmdJson)
                || cmdJson.GetString() is not { Length: >0 } cmdStr)
            {
                    
#if DEBUG
                Debug.WriteLine("Parse failed! The 'cmd' property is missing.");
#endif
                return null;
            }

            if (!Enum.TryParse<KnownCommands>(cmdStr, true, out var command))
            {
#if DEBUG
                Debug.WriteLine($"Unknown command: {raw}");
#endif
                return null;
            }
                
            var cmd = rawCmd.Deserialize<CommandBase<JsonElement>>(JsonSerializerOptions);
            if (!CommandTypeMapping.TryGetValue(cmd.Command, out var underlying)) return cmd;
                
            var converter = GetCommandConverter(underlying);
            return converter(cmd, JsonSerializerOptions);
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