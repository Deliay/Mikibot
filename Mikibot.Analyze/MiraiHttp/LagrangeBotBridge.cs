using System.Globalization;
using System.Text.Json;
using Lagrange.Core;
using Lagrange.Core.Common;
using Lagrange.Core.Common.Interface;
using Lagrange.Core.Common.Interface.Api;
using Lagrange.Core.Event.EventArg;
using Lagrange.Core.Message;
using Lagrange.Core.Message.Entity;
using Microsoft.Extensions.Logging;
using Mirai.Net.Data.Messages;
using Mirai.Net.Data.Messages.Concretes;
using Mirai.Net.Data.Messages.Receivers;
using Mirai.Net.Data.Shared;
using File = System.IO.File;
using LogLevel = Lagrange.Core.Event.EventArg.LogLevel;
using MessageChain = Lagrange.Core.Message.MessageChain;

namespace Mikibot.Analyze.MiraiHttp;

public class LagrangeBotBridge(ILogger<LagrangeBotBridge> logger, ILogger<BotContext> botLogger) : IBotService, ILagrangeBot
{
    private static readonly string BotConfigDir = Environment.GetEnvironmentVariable("BOT_CONFIG_DIR") ?? Path.GetTempPath();

    private static readonly string BotLoginStore = Path.Combine(BotConfigDir, "login.json");
    private static readonly string BotDeviceStore = Path.Combine(BotConfigDir, "device.json");

    private static async ValueTask<BotKeystore?> GetKeyStore()
    {
        if (!File.Exists(BotLoginStore)) return null;
        
        await using var fileStream = File.OpenRead(BotLoginStore);
        return await JsonSerializer.DeserializeAsync<BotKeystore>(fileStream);
    }
    private static async ValueTask SaveKeyStore(BotKeystore keystore)
    {
        if (File.Exists(BotLoginStore))
        {
            File.Move(BotLoginStore, BotLoginStore + "-" + Guid.NewGuid());
        }
        
        await using var fileStream = File.OpenWrite(BotLoginStore);
        await JsonSerializer.SerializeAsync(fileStream, keystore);
    }
    
    private static async ValueTask<BotDeviceInfo> GetDeviceInfo()
    {
        if (!File.Exists(BotDeviceStore))
        {
            var info = BotDeviceInfo.GenerateInfo();
            info.DeviceName = "Generic x86 System";
            info.KernelVersion = "6.6.7-arch1-1";
            info.SystemKernel = "Linux";
            
            await using var file = File.OpenWrite(BotDeviceStore);
            await JsonSerializer.SerializeAsync(file, info);
            
            return info;
        }
        
        await using var fileStream = File.OpenRead(BotDeviceStore);
        var result = await JsonSerializer.DeserializeAsync<BotDeviceInfo>(fileStream);
        return result ?? new BotDeviceInfo();
    }

    private BotContext bot = null!;

    private async ValueTask WaitBotOnlineAsync()
    {
        var tcs = new TaskCompletionSource();
        
        bot.Invoker.OnBotOnlineEvent += InvokerOnOnBotOnlineEvent;

        try
        {
            await tcs.Task;
        }
        finally
        {
            bot.Invoker.OnBotOnlineEvent -= InvokerOnOnBotOnlineEvent;
        }

        return;
        void InvokerOnOnBotOnlineEvent(BotContext context, BotOnlineEvent @event)
        {
            tcs.TrySetResult();
        }
    }

    public HttpClient HttpClient { get; } = new();

    public async ValueTask Run(CancellationToken cancellationToken = default)
    {
        var botKeystore = await GetKeyStore();
        bot = BotFactory.Create(new BotConfig()
        {
            Protocol = Protocols.Linux,
        }, await GetDeviceInfo(), new BotKeystore());
        bot.Invoker.OnBotLogEvent += (_, args) => botLogger.Log(args.Level switch
        {
            LogLevel.Debug => Microsoft.Extensions.Logging.LogLevel.Trace,
            LogLevel.Verbose => Microsoft.Extensions.Logging.LogLevel.Information,
            LogLevel.Information => Microsoft.Extensions.Logging.LogLevel.Information,
            LogLevel.Warning => Microsoft.Extensions.Logging.LogLevel.Warning,
            LogLevel.Fatal => Microsoft.Extensions.Logging.LogLevel.Error,
            _ => Microsoft.Extensions.Logging.LogLevel.Error
        }, args.ToString());
        bot.Invoker.OnBotCaptchaEvent += (_, @event) =>
        {
            Console.WriteLine(@event.ToString());
            var captcha = Console.ReadLine();
            var randStr = Console.ReadLine();
            if (captcha != null && randStr != null) bot.SubmitCaptcha(captcha, randStr);
        };
        bot.Invoker.OnGroupMessageReceived += InvokerOnOnGroupMessageReceived;

        if (botKeystore is null)
        {

            var result = await bot.FetchQrCode();
            if (!result.HasValue) throw new InvalidOperationException("Lagrange无法获得二维码");
        
            logger.LogInformation("需要扫码登录，二维码图片[png]：base64={}", Convert.ToBase64String(result.Value.QrCode));
            await bot.LoginByQrCode();
            await SaveKeyStore(bot.UpdateKeystore());
        }
        else
        {
            await bot.LoginByPassword();
        }
        
        logger.LogInformation("等待登录中...");
        await WaitBotOnlineAsync();
        logger.LogInformation("登录完成");
    }

    private static IEnumerable<MessageBase> ConvertMessageToMiraiCore(MessageChain lagrange)
    {
        foreach (var messageEntity in lagrange)
        {
            if (messageEntity is TextEntity textEntity)
            {
                yield return new PlainMessage()
                {
                    Text = textEntity.Text,
                };
            }
        }
    }

    private static IEnumerable<IMessageEntity> ConvertMessageToLagrangeCore(Mirai.Net.Data.Messages.MessageChain mirai)
    {
        foreach (var messageBase in mirai)
        {
            switch (messageBase)
            {
                case PlainMessage plainMessage:
                    yield return new TextEntity()
                    {
                        Text = plainMessage.Text,
                    };
                    break;
                case ImageMessage { Base64: not null } imageMessage:
                    yield return new ImageEntity(Convert.FromBase64String(imageMessage.Base64));
                    break;
                case ImageMessage { Url: not null } imageMessage:
                    yield return new ImageEntity() { ImageUrl = imageMessage.Url };
                    break;
                case ImageMessage imageMessage:
                {
                    if (imageMessage.Path is not null)
                    {
                        yield return new ImageEntity(imageMessage.Path);
                    }

                    break;
                }
                case VoiceMessage voiceMessage:
                    yield return new RecordEntity(Convert.FromBase64String(voiceMessage.Base64));
                    break;
                default:
                    continue;
            }
        }
    }

    private static Mirai.Net.Data.Messages.MessageChain ConvertMessageToMirai(MessageChain lagrange)
    {
        return new Mirai.Net.Data.Messages.MessageChain(ConvertMessageToMiraiCore(lagrange));
    }

    private static MessageChain ConvertMessageToLagrange(uint uid, Mirai.Net.Data.Messages.MessageChain mirai)
    {
        var builder = MessageBuilder.Group(uid);
        foreach (var messageEntity in ConvertMessageToLagrangeCore(mirai))
        {
            builder.Add(messageEntity);
        }
        return builder.Build();
    }
    
    private void InvokerOnOnGroupMessageReceived(BotContext context, GroupMessageEvent e)
    {
        if (e.Chain.Type != MessageChain.MessageType.Group) return;

        var groupId = e.Chain.GroupUin!.Value;
        var sender = e.Chain.GroupMemberInfo!;
        
        foreach (var (next, _) in _subscriber)
        {
            next(new GroupMessageReceiver()
            {
                Type = MessageReceivers.Group,
                Sender = new Member()
                {
                    Group = new Group()
                    {
                        Id = $"{groupId}",
                        Name = "",
                        Permission = (Permissions)sender.Permission,
                    },
                    Id = $"{sender.Uin}",
                    Name = sender.MemberName,
                    Permission = (Permissions)sender.Permission,
                    JoinTime = sender.JoinTime.ToString(CultureInfo.InvariantCulture),
                    SpecialTitle = sender.MemberCard,
                    LastSpeakTime = sender.LastMsgTime.ToString(CultureInfo.InvariantCulture),
                },
                MessageChain = ConvertMessageToMirai(e.Chain)
            });
        }
    }

    private static readonly HashSet<string> AllowGroups =
    [
        "314503649",
        "139528984"
    ];
    private readonly SemaphoreSlim _lock = new(1);
    private DateTimeOffset latestSentAt = DateTimeOffset.Now;

    public async ValueTask SendMessageToGroup(Group group, CancellationToken token, params MessageBase[] messages)
    {
        await bot.SendMessage(ConvertMessageToLagrange(uint.Parse(group.Id), new Mirai.Net.Data.Messages.MessageChain(messages)));
    }

    private readonly Dictionary<Action<GroupMessageReceiver>, CancellationTokenRegistration> _subscriber = [];

    public string UserId => throw new NotImplementedException();

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

    public async ValueTask<Dictionary<string, string>> SendMessageToSomeGroup(HashSet<string> groupIds, CancellationToken token, params MessageBase[] messages)
    {
        var groups = await bot.FetchGroups();
        foreach (var group in groups)
        {
            var groupId = $"{group.GroupUin}";
#if DEBUG
            if (groupId != "139528984") continue;
#endif
            if (token.IsCancellationRequested) break;

            if (groupIds.Contains(groupId)) {
                await SendMessageToGroup(new Group { Id = groupId, }, token, messages);
            }
        }

        return [];
    }

    public async ValueTask<bool> ReactionGroupMessageAsync(string groupId, string messageId, string emotionId, bool isAdd = true, CancellationToken cancellationToken = default)
    {
        return await bot.GroupSetMessageReaction(uint.Parse(groupId), uint.Parse(messageId), emotionId, isAdd);
    }
}
