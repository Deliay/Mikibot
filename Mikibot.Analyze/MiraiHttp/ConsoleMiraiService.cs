using Microsoft.Extensions.Logging;
using Mirai.Net.Data.Messages;
using Mirai.Net.Data.Messages.Concretes;
using Mirai.Net.Data.Messages.Receivers;
using Mirai.Net.Data.Shared;

namespace Mikibot.Analyze.MiraiHttp;

public class ConsoleMiraiService : IMiraiService
{
    public ConsoleMiraiService(ILogger<ConsoleMiraiService> logger)
    {
        Logger = logger;
    }

    public ILogger<ConsoleMiraiService> Logger { get; }

    public ValueTask Run()
    {
        Logger.LogInformation("Console mirai service started");
        return ValueTask.CompletedTask;
    }

    public ValueTask SendMessageToAllGroup(CancellationToken token, params MessageBase[] messages)
    {
        Logger.LogInformation("{}", string.Join("", (object[])messages));
        return ValueTask.CompletedTask;
    }

    public ValueTask SendMessageToGroup(Group group, CancellationToken token, params MessageBase[] messages)
    {
        return SendMessageToAllGroup(token, messages);
    }

    public ValueTask SendMessageToSliceManGroup(CancellationToken token, params MessageBase[] messages)
    {
        return SendMessageToAllGroup(token, messages);
    }

    public ValueTask SendMessageToSomeGroup(HashSet<string> groupIds, CancellationToken token, params MessageBase[] messages)
    {
        return SendMessageToAllGroup(token, messages);
    }

    public void SubscribeMessage(Action<GroupMessageReceiver> next, CancellationToken token)
    {
        Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                var text = Console.ReadLine();
                if (text == null || text.Length == 0 || text.Contains("114514"))
                {
                    Logger.LogInformation("aaaaa");
                    continue;
                }

                next(new GroupMessageReceiver()
                {
                    Sender = new Member()
                    {
                        Id = "644676751",
                        Group = new Group()
                        {
                            Id = "139528984",
                            Name = "Mikibot内测群",
                        }
                    },
                    MessageChain = new MessageChain(new List<MessageBase>
                    {
                        new PlainMessage()
                        {
                            Text = text,
                        }
                    }),
                });
                await Task.Delay(1000, token);
            }
        }, token);
    }
}