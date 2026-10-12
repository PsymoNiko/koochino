using Microsoft.AspNetCore.SignalR;
using MyShop.Application.Interfaces.SupportChat;
using MyShop.Core.Models.SupportChat;
using MyShop.Web.Hubs;
using System;
using System.Threading.Tasks;

namespace MyShop.Web.Services
{
    public class SupportChatRealtimePublisher : ISupportChatRealtimePublisher
    {
        private readonly IHubContext<SupportChatHub> _hub;

        public SupportChatRealtimePublisher(IHubContext<SupportChatHub> hub) => _hub = hub;

        public async Task NewMessageAsync(Guid chatId, int? siteId, SupportChatMessage message)
        {
            var payload = new SupportChatRealtimeMessage
            {
                ChatId = chatId,
                Id = message.Id,
                SenderType = message.SenderType,
                SenderUserId = message.SenderUserId,
                Body = message.Body,
                MessageType = message.MessageType,
                AttachmentUrl = message.AttachmentUrl,
                AttachmentName = message.AttachmentName,
                AttachmentSize = message.AttachmentSize,
                CreatedAt = message.CreatedAt,
                ReadAt = message.ReadAt
            };
            await _hub.Clients.Group(Conv(chatId)).SendAsync("NewMessage", payload);
            await FanoutOpsAsync(siteId, "NewMessage", payload);
            await FanoutOpsAsync(siteId, "InboxChanged", new { chatId, siteId });
        }

        public Task MessageReadAsync(Guid chatId, string readerSide) =>
            _hub.Clients.Group(Conv(chatId)).SendAsync("MessageRead", new { chatId, readerSide });

        public Task TypingAsync(Guid chatId, string senderSide, bool isTyping) =>
            _hub.Clients.Group(Conv(chatId)).SendAsync("Typing", new { chatId, senderSide, isTyping });

        public async Task ConversationUpdatedAsync(Guid chatId, int? siteId, string status)
        {
            var payload = new { chatId, siteId, status };
            await _hub.Clients.Group(Conv(chatId)).SendAsync("ConversationUpdated", payload);
            await FanoutOpsAsync(siteId, "ConversationUpdated", payload);
            await FanoutOpsAsync(siteId, "InboxChanged", payload);
        }

        public Task OperatorNotifyAsync(int? siteId, SupportOperatorNotification notification) =>
            FanoutOpsAsync(siteId, "OperatorNotify", notification);

        async Task FanoutOpsAsync(int? siteId, string method, object payload)
        {
            await _hub.Clients.Group(Ops(siteId)).SendAsync(method, payload);
            if ((siteId ?? 0) != 0)
                await _hub.Clients.Group(Ops(null)).SendAsync(method, payload);
        }

        static string Conv(Guid chatId) => "conv:" + chatId.ToString("N");
        static string Ops(int? siteId) => "ops:site:" + (siteId ?? 0);
    }
}
