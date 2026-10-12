using MyShop.Core.Models.SupportChat;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Interfaces.SupportChat
{
    public interface ISupportChatRepository
    {
        Task<SupportChatThread> GetAsync(Guid id);
        Task<SupportChatThread> GetOpenAsync(int? siteId, int? userId, string guestToken);
        Task<SupportChatThread> CreateAsync(SupportChatThread row);
        Task<List<SupportChatThread>> ListForAdminAsync(int? siteId, string status, int take = 80);
        Task AddMessageAsync(SupportChatMessage msg);
        Task<List<SupportChatMessage>> ListMessagesAsync(Guid chatId, long afterId = 0);
        Task TouchAsync(Guid chatId, string preview, string status, bool incrementAdminUnread, bool incrementUserUnread, bool clearAdminUnread, bool clearUserUnread, int? assignedAdminUserId, string currentPageUrl = null);
        Task CloseAsync(Guid chatId);
        Task MarkMessagesReadAsync(Guid chatId, string readerSide);
        Task UpdatePageAsync(Guid chatId, string pageUrl);
        Task<SupportSettings> GetSettingsAsync(int siteId);
        Task UpsertSettingsAsync(SupportSettings settings);
    }

    public interface ISupportChatRealtimePublisher
    {
        Task NewMessageAsync(Guid chatId, int? siteId, SupportChatMessage message);
        Task MessageReadAsync(Guid chatId, string readerSide);
        Task TypingAsync(Guid chatId, string senderSide, bool isTyping);
        Task ConversationUpdatedAsync(Guid chatId, int? siteId, string status);
        Task OperatorNotifyAsync(int? siteId, SupportOperatorNotification notification);
    }

    public interface ISupportChatService
    {
        Task<SupportChatThread> StartOrGetAsync(SupportChatStartRequest request);
        Task<SupportChatMessage> SendFromUserAsync(Guid chatId, int? userId, string body, string messageType = null, string attachmentUrl = null, string attachmentName = null, int? attachmentSize = null);
        Task<SupportChatMessage> SendFromAdminAsync(Guid chatId, int adminUserId, string body, string messageType = null, string attachmentUrl = null, string attachmentName = null, int? attachmentSize = null);
        Task<List<SupportChatMessage>> GetMessagesAsync(Guid chatId, long afterId = 0);
        Task<SupportChatThread> GetAsync(Guid id);
        Task<List<SupportChatThread>> ListInboxAsync(int? siteId, string status = null);
        Task CloseAsync(Guid chatId);
        Task MarkReadByUserAsync(Guid chatId);
        Task MarkReadByAdminAsync(Guid chatId, int adminUserId);
        Task NotifyTypingAsync(Guid chatId, string senderSide, bool isTyping);
        Task UpdateCurrentPageAsync(Guid chatId, string pageUrl);
        Task<SupportSettings> GetSettingsAsync(int siteId);
        Task SaveSettingsAsync(SupportSettings settings);
    }
}
