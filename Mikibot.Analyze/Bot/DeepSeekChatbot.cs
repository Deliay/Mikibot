using Microsoft.Extensions.Logging;
using Mikibot.Analyze.Generic;
using Mikibot.Analyze.MiraiHttp;
using Mirai.Net.Data.Messages.Concretes;
using Mirai.Net.Data.Messages.Receivers;
using Mirai.Net.Data.Shared;
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
    private readonly PermissionService permissions;
    private const string BasicPrompt = "请你扮演美少女弥系Miki，她是个萌音二次云美少女，" +
        "说话风格非常雌小鬼，令人拳头发硬。" +
        "帮我分析user的聊天记录，其中每一行是一个人说的一句话，给出你对群内话题感兴趣程度的分值，" +
        "并给出你的回复，你的回复字数在15字之内" +
        "且你单个回复的内容只能选择一个话题，并且以自然的方式进行回复，能参与到群聊中不被认出是机器人，" +
        "如果有不认识的上下文，最好结合网络的搜索资料来进行思考，最好少使用或不使用颜文字，回复也尽量口语化，" +
        "最好是使用能挑起话题的语气（比如锐评），请尽量分析上下文中有可能的主题，以 JSON数组的形式输出，" +
        "格式为：[{ \"score\": 60, \"reply\": \"the reply message when score > 75\", \"topic\": \"the topic which you found\" }, " +
        "{ \"score\": 75, \"reply\": \"the reply message when score > 75\", \"topic\": \"the topic which you found\" }...]";

    public DeepSeekChatbot(IMiraiService miraiService,
        ILogger<DeepSeekChatbot> logger,
        PermissionService permissions) : base(miraiService, logger)
    {
        _httpClient = new HttpClient();
        var token = Environment.GetEnvironmentVariable("DEEPSEEK_TOKEN")
            ?? throw new ArgumentException("DeepSeek API token not configured");
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
        this.permissions = permissions;
    }
    private const string Chatbot = "Chatbot";
    protected override async ValueTask Process(GroupMessageReceiver message, CancellationToken token = default)
    {
        var group = message.Sender.Group;

        if (!await permissions.IsGroupEnabled(Chatbot, group.Id, token)) return;

        if (!_recentMessages.TryGetValue(group.Id, out var messages))
        {
            _recentMessages.Add(group.Id, messages = []);
        }

        foreach (var item in message.MessageChain)
        {
            if (item is PlainMessage plain)
            {
                messages.Enqueue($"{message.Sender.Name}: {plain.Text}\n");
            }
        }

        await TryResponse(message.GroupId, token);
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
    }
    private record Message(string role, string content);
    private record Chat(List<Message> messages,
        string model = "deepseek-chat",
        double temperature = 1.3,
        bool search_enabled = true);
    private record Choice(Message message);
    private record Response(List<Choice> choices);

    private record GroupChatResponse(int score, string topic, string reply);

    private async ValueTask TryResponse(string groupId, CancellationToken cancellationToken)
    {

        if (!_recentMessages.TryGetValue(groupId, out var messages)) return;

        await BeginLock(groupId, async (cancellationToken) =>
        {
            if (messages.Count < 15) return;


            string messageList = "";

            while (messages.TryDequeue(out var message))
            {
                messageList += message;
            }

            var res = await _httpClient.PostAsJsonAsync("https://api.deepseek.com/chat/completions", new Chat(
                [
                    new Message("system", BasicPrompt),
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
                var groupChats = JsonSerializer.Deserialize<List<GroupChatResponse>>(content);
                if (groupChats is null) return;

                var interestChat = groupChats.MaxBy(c => c.score);
                if (interestChat is null) return;

                await MiraiService.SendMessageToSomeGroup([groupId], default,
                    new PlainMessage(interestChat.reply));

                Logger.LogInformation("Deekseep bot: {}", content);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An exception thrown when submitting to deepseek");
            }
        }, cancellationToken);

    }
}