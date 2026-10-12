using MyShop.Application.Interfaces.SupportChat;
using MyShop.Core.Models.SupportChat;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Web.Services
{
    /// <summary>پس از شروع/ادامه چت وب، کوکی دسترسی را بدون تغییر UI صادر می‌کند.</summary>
    public class SupportChatCookieBinder : ISupportChatService
    {
        private readonly ISupportChatService _inner;
        private readonly SupportChatAccessTokenService _tokens;

        public SupportChatCookieBinder(ISupportChatService inner, SupportChatAccessTokenService tokens)
        {
            _inner = inner;
            _tokens = tokens;
        }

        public async Task<SupportChatThread> StartOrGetAsync(SupportChatStartRequest request)
        {
            var thread = await _inner.StartOrGetAsync(request);
            if (thread != null)
                _tokens.Issue(thread.Id, thread.SiteId ?? 0);
            return thread;
        }

        public async Task<SupportChatMessage> SendFromUserAsync(Guid chatId, int? userId, string body, string messageType = null, string attachmentUrl = null, string attachmentName = null, int? attachmentSize = null)
        {
            var msg = await _inner.SendFromUserAsync(chatId, userId, body, messageType, attachmentUrl, attachmentName, attachmentSize);
            var chat = await _inner.GetAsync(chatId);
            if (chat != null)
                _tokens.Issue(chat.Id, chat.SiteId ?? 0);
            return msg;
        }

        public Task<SupportChatMessage> SendFromAdminAsync(Guid chatId, int adminUserId, string body, string messageType = null, string attachmentUrl = null, string attachmentName = null, int? attachmentSize = null)
            => _inner.SendFromAdminAsync(chatId, adminUserId, body, messageType, attachmentUrl, attachmentName, attachmentSize);

        public Task<List<SupportChatMessage>> GetMessagesAsync(Guid chatId, long afterId = 0)
            => _inner.GetMessagesAsync(chatId, afterId);

        public Task<SupportChatThread> GetAsync(Guid id) => _inner.GetAsync(id);

        public Task<List<SupportChatThread>> ListInboxAsync(int? siteId, string status = null)
            => _inner.ListInboxAsync(siteId, status);

        public Task CloseAsync(Guid chatId) => _inner.CloseAsync(chatId);

        public Task MarkReadByUserAsync(Guid chatId) => _inner.MarkReadByUserAsync(chatId);

        public Task MarkReadByAdminAsync(Guid chatId, int adminUserId) => _inner.MarkReadByAdminAsync(chatId, adminUserId);

        public Task NotifyTypingAsync(Guid chatId, string senderSide, bool isTyping)
            => _inner.NotifyTypingAsync(chatId, senderSide, isTyping);

        public Task UpdateCurrentPageAsync(Guid chatId, string pageUrl) => _inner.UpdateCurrentPageAsync(chatId, pageUrl);

        public Task<SupportSettings> GetSettingsAsync(int siteId) => _inner.GetSettingsAsync(siteId);

        public Task SaveSettingsAsync(SupportSettings settings) => _inner.SaveSettingsAsync(settings);
    }
}
