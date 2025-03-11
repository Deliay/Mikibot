using Microsoft.Extensions.Logging;
using Mikibot.Analyze.Generic;
using Mikibot.Analyze.MiraiHttp;
using Mikibot.Analyze.Service.Ai;
using Mirai.Net.Data.Messages.Concretes;
using Mirai.Net.Data.Messages.Receivers;

namespace Mikibot.Analyze.Bot;

public class ChatbotManagerService(
    IQqService qqService,
    ILogger<ChatbotManagerService> logger,
    PermissionService permissionService,
    ChatbotSwitchService chatbotSwitchService)
    : MiraiGroupMessageProcessor<ChatbotManagerService>(qqService, logger)
{

    private const string VendorCommand = "/chatbot_vendor";
    protected override async ValueTask Process(GroupMessageReceiver message, CancellationToken token = default)
    {
        foreach (var plainMessage in message.MessageChain.OfType<PlainMessage>())
        {
            var msg = plainMessage.Text.Trim();
            if (!msg.StartsWith(VendorCommand)) return;

            if (!await permissionService.IsBotOperator(message.Sender.Id, token)) return;
            
            var vendor = msg[VendorCommand.Length..];

            if (!chatbotSwitchService.UpdateChatbot(vendor)) continue;
            
            await QqService.SendMessageToSomeGroup([message.GroupId], token,
                new PlainMessage($"机器人后端成功更换为 {vendor}"));

            return;
        }
        
        
    }
}