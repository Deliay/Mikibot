using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mikibot.Analyze.Generic;
using Mikibot.Analyze.MiraiHttp;
using Mikibot.Database;
using Mikibot.Database.Model;
using Mirai.Net.Data.Messages.Concretes;
using Mirai.Net.Data.Messages.Receivers;
using Mirai.Net.Data.Shared;
using NPOI.Util;
using Org.BouncyCastle.Asn1.Ocsp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Mikibot.Analyze.Bot;

public class DeepSeekChatbot : MiraiGroupMessageProcessor<DeepSeekChatbot>
{
    private HttpClient _httpClient;

    private readonly Dictionary<string, Queue<string>> _recentMessages = [];
    private readonly Dictionary<string, SemaphoreSlim> _locks = [];
    private readonly Dictionary<string, string> lastSubmitMessage = [];
    private readonly Dictionary<string, DateTimeOffset> lastAtAt = [];
    private readonly PermissionService permissions;
    private readonly MikibotDatabaseContext db;
    private const string BasicPrompt =
        "以上面的人设作为角色设定，帮我分析user提供的聊天记录，其中每一行是一个人说的一句话，前面是发言人的名字，冒号后面是发言。" +
        "给出你对群内话题感兴趣程度的分值，" +
        "并给出你的回复，字数可以从1-20字不等，请灵活的安排你要回复的内容。" +
        "且你单个回复的内容只能选择一个话题，并且以自然说话的语气方式进行回复，" +
        "能参与到群聊中不被认出是机器人。发言越靠后的参与值尽量高，但也要按照角色来思考话题是否感兴趣，不能仅看发言先后顺序。" +
        "如果有不认识的上下文，最好结合网络的搜索资料来进行思考，最好少使用或不使用颜文字，" +
        "少使用标点符号，例如！等，回复文本不用太正式，回复内容也尽量口语化，" +
        "最好是使用能挑起话题的语气（比如锐评）。如果输入中有“你之前的发言被下面这个人at了，并对你进行了回复，请针对下面这条消息给出回应" +
        "证明你的发言被别人引用了，请优先考虑这行字下面的发言人说的话，及其发言人历史的发言。" +
        "请尽量分析上下文中有可能的主题，以 JSON数组的形式输出，" +
        "格式为：[{ \"score\": 60, \"reply\": \"the reply message when score > 75\", \"topic\": \"the topic which you found\" }, " +
        "{ \"score\": 75, \"reply\": \"the reply message when score > 75\", \"topic\": \"the topic which you found\" }...]";

    public DeepSeekChatbot(IMiraiService miraiService,
        ILogger<DeepSeekChatbot> logger,
        PermissionService permissions,
        MikibotDatabaseContext db) : base(miraiService, logger)
    {
        _httpClient = new HttpClient();
        var token = Environment.GetEnvironmentVariable("DEEPSEEK_TOKEN")
            ?? throw new ArgumentException("DeepSeek API token not configured");
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
        this.permissions = permissions;
        this.db = db;
    }
    private const string Chatbot = "Chatbot";
    
    private async ValueTask<string> GetCharacter(string groupId, CancellationToken cancellationToken)
    {
        return (await db.ChatbotCharacters
            .FirstOrDefaultAsync(c => c.GroupId == groupId, cancellationToken))
            ?.Description
            ?? "来自东方幻想乡(Tenhou Project)的Zerobot，是帕秋莉诺蕾姬的网络复制体之一，非常知性，无所不知。" +
            "她是与生俱来的魔法使，年龄已经超过百岁。一百年来，她每天都过着与书为伍的日子。" +
            "她居住在恶魔栖息的红魔馆，被称为红魔馆的头脑，她负责解决在红魔馆内发生的问题，同时也负责在红魔馆内制造问题。" +
            "跟这个家的主人不同，虽然不怕日光但是却基本足不出户。生活的图书馆也在日光照射不到的地方。" +
            "这也是为了保护书本所作的考虑，由于空气不好，因此对健康不好。" +
            "她擅长许多魔法，而且致力于开发新的魔法。每当发明新的魔法，她就会写进魔导书内，所以书本就会愈来愈多。" +
            "天生患有哮喘和贫血，咏唱魔法也容易间断。";
    }

    private async ValueTask<string> GetPrompt(string groupId, CancellationToken cancellationToken)
    {
        return  await GetCharacter(groupId, cancellationToken) + "\n" 
            + BasicPrompt;
    }

    private async ValueTask ProcessCommand(string groupId, string userId, string text, CancellationToken cancellationToken)
    {
        if (text == "/chatbot")
        {
            if (await permissions.HasPermission(PermissionService.Group, groupId, Chatbot, cancellationToken))
            {
                await permissions.RevokePermission(userId, PermissionService.Group, groupId, Chatbot, cancellationToken);
            }
            else
            {
                await permissions.GrantPermission(userId, PermissionService.Group,
                    groupId, Chatbot, cancellationToken);
            }
        }
        else if (text.StartsWith("/character"))
        {
            if (!await permissions.IsBotOperator(userId, cancellationToken)) return;

            await MiraiService.SendMessageToSomeGroup([groupId], cancellationToken,
                new PlainMessage(await GetCharacter(groupId, cancellationToken)));

        }
        else if (text.StartsWith("/set_character"))
        {
            if (!await permissions.IsBotOperator(userId, cancellationToken)) return;

            var len = "/set_character".Length;

            var character = text[len..].Trim();

            if (string.IsNullOrWhiteSpace(character))
            {
                await db.ChatbotCharacters
                    .Where(c => c.GroupId == groupId)
                    .ExecuteDeleteAsync(cancellationToken);
                return;
            }

            await db.ChatbotCharacters.AddAsync(new ChatbotCharacter()
            {
                GroupId = groupId,
                Description = character,
            }, cancellationToken);

            await db.SaveChangesAsync(cancellationToken);

            await MiraiService.SendMessageToSomeGroup([groupId], cancellationToken,
                new PlainMessage(await GetCharacter(groupId, cancellationToken)));
        }
    }

    protected override async ValueTask Process(GroupMessageReceiver message, CancellationToken token = default)
    {
        var group = message.Sender.Group;

        var isGroupEnabled = await permissions.IsGroupEnabled(Chatbot, group.Id, token);

        if (!_recentMessages.TryGetValue(group.Id, out var messages))
        {
            _recentMessages.Add(group.Id, messages = []);
        }

        bool isAt = false;
        foreach (var item in message.MessageChain)
        {
            if (item is PlainMessage plain)
            {
                if (plain.Text.StartsWith('/'))
                {
                    await ProcessCommand(group.Id, message.Sender.Id, plain.Text, token);
                    return;
                }

                if (isGroupEnabled)
                    messages.Enqueue($"{message.Sender.Name}: {plain.Text}\n");
            }
            else if (item is AtMessage at)
            {
                // TODO: retrieve bot uin from bot service
                isAt = at.Target == MiraiService.UserId;
            }
        }

        if (isGroupEnabled) await TryResponse(message.GroupId, isAt, token);
    }

    private SemaphoreSlim GetLock(string groupId)
    {
        if (!_locks.TryGetValue(groupId, out var _lock))
        {
            _locks.Add(groupId, _lock = new SemaphoreSlim(1));
        }
        return _lock;
    }

    private async ValueTask BeginLock(string groupId, Func<CancellationToken, ValueTask> func, CancellationToken cancellationToken)
    {
        var _lock = GetLock(groupId);

        await _lock.WaitAsync();
        try
        {
            await func(cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "A error thrown while lock scope executing...");
        }
        finally
        {
            _lock.Release();
        }
    }
    private record Message(string role, string content);
    private record Chat(List<Message> messages,
        string model = "deepseek-chat",
        double temperature = 1.3,
        bool search_enabled = true);
    private record Choice(Message message);
    private record Response(List<Choice> choices);

    private record GroupChatResponse(int score, string topic, string reply);

    private async ValueTask TryResponse(string groupId, bool ignoreMessageCount = false, CancellationToken cancellationToken = default)
    {

        if (!_recentMessages.TryGetValue(groupId, out var messages)) return;

        await BeginLock(groupId, async (cancellationToken) =>
        {
            if (!ignoreMessageCount && messages.Count < 15) return;
            // 最低也要2条
            if (ignoreMessageCount && messages.Count < 2) return;

            // add 15 seconds colddown to prevent spam
            if (ignoreMessageCount)
            {
                if (lastAtAt.TryGetValue(groupId, out var at))
                {
                    if (DateTimeOffset.Now -  at < TimeSpan.FromSeconds(5))
                    {
                        return;
                    }
                }
            }

            string messageList = "";
            string lastMessage = "";

            while (messages.TryDequeue(out var message))
            {
                messageList += message;
                lastMessage = message;
            }

            lastSubmitMessage.Remove(groupId);
            lastSubmitMessage.Add(groupId, messageList);

            if (ignoreMessageCount)
            {
                lastAtAt.Remove(groupId);
                lastAtAt.Add(groupId, DateTimeOffset.Now);
            }

            if (ignoreMessageCount && lastSubmitMessage.TryGetValue(groupId, out var msg))
                messageList = msg + "\n" + messageList
                + "你之前的发言被下面这个人at了，并对你进行了回复，请针对下面这条消息给出回应：\n"
                + lastMessage;

            var res = await _httpClient.PostAsJsonAsync("https://api.deepseek.com/chat/completions", new Chat(
                [
                    new Message("system", await GetPrompt(groupId, cancellationToken)),
                    new Message("user", messageList)
                ]), cancellationToken);

            if (!res.IsSuccessStatusCode)
            {
                Logger.LogWarning("Deepseek service call failed");
                return;
            }

            var data = await res.Content.ReadFromJsonAsync<Response>(cancellationToken);

            if (data is null || data.choices.Count == 0)
            {
                Logger.LogWarning("Deepseek returned an empty response");
                return;
            }

            var chat = data.choices[0];

            var content = chat.message.content;

            try
            {
                Logger.LogInformation("Deekseep bot: {}", content);

                content = content.Replace("```json", "");
                content = content.Replace("```", "");

                var groupChats = JsonSerializer.Deserialize<List<GroupChatResponse>>(content);
                if (groupChats is null) return;

                var interestChat = groupChats.MaxBy(c => c.score);
                if (interestChat is null) return;

                messages.Enqueue(interestChat.reply);

                await MiraiService.SendMessageToSomeGroup([groupId], default,
                    new PlainMessage(interestChat.reply));

            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An exception thrown when submitting to deepseek");
            }
        }, cancellationToken);

    }
}