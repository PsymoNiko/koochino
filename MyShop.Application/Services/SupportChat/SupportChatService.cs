using MyShop.Application.Interfaces.SupportChat;
using MyShop.Core.Models.SupportChat;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Services.SupportChat
{
    public class SupportChatService : ISupportChatService
    {
        private readonly ISupportChatRepository _repo;
        private readonly ISupportChatRealtimePublisher _realtime;
        private readonly ISupportDeskRepository _desk;
        private readonly ISupportAssistService _assist;
        private readonly ISupportAnalyticsRepository _analytics;
        private readonly ISupportAutoReplyService _autoReply;
        private readonly ISupportChannelOutbound _outbound;

        public SupportChatService(
            ISupportChatRepository repo,
            ISupportChatRealtimePublisher realtime = null,
            ISupportDeskRepository desk = null,
            ISupportAssistService assist = null,
            ISupportAnalyticsRepository analytics = null,
            ISupportAutoReplyService autoReply = null,
            ISupportChannelOutbound outbound = null)
        {
            _repo = repo;
            _realtime = realtime;
            _desk = desk;
            _assist = assist;
            _analytics = analytics;
            _autoReply = autoReply;
            _outbound = outbound;
        }

        public async Task<SupportChatThread> StartOrGetAsync(SupportChatStartRequest request)
        {
            request ??= new SupportChatStartRequest();
            var siteId = request.SiteId ?? 0;

            if (_desk != null)
            {
                var blocked = await _desk.IsBlockedAsync(request.SiteId, request.UserId, request.GuestToken, request.VisitorPhone);
                if (blocked)
                    throw new InvalidOperationException("دسترسی شما به پشتیبانی محدود شده است.");
            }

            if (siteId > 0)
            {
                var settings = await _repo.GetSettingsAsync(siteId);
                if (settings != null && !settings.IsChatEnabled)
                    throw new InvalidOperationException(settings.OfflineMessage ?? "چت پشتیبانی فعلاً غیرفعال است.");
            }

            var existing = await _repo.GetOpenAsync(request.SiteId, request.UserId, request.GuestToken);
            if (existing != null)
            {
                if (!string.IsNullOrWhiteSpace(request.CurrentPageUrl))
                    await _repo.UpdatePageAsync(existing.Id, request.CurrentPageUrl);
                return await _repo.GetAsync(existing.Id);
            }

            var welcome = "گفتگو با پشتیبانی شروع شد. پیام خود را بنویسید؛ به‌زودی پاسخ داده می‌شود.";
            if (siteId > 0)
            {
                try
                {
                    var settings = await _repo.GetSettingsAsync(siteId);
                    if (!string.IsNullOrWhiteSpace(settings?.WelcomeMessage))
                        welcome = settings.WelcomeMessage.Trim();
                }
                catch { /* جدول تنظیمات ممکن است هنوز نباشد */ }
            }

            var created = await _repo.CreateAsync(new SupportChatThread
            {
                SiteId = request.SiteId,
                UserId = request.UserId,
                GuestToken = string.IsNullOrWhiteSpace(request.GuestToken) ? null : request.GuestToken.Trim(),
                VisitorName = Trunc(request.VisitorName, 120),
                VisitorPhone = Trunc(request.VisitorPhone, 32),
                Audience = string.IsNullOrWhiteSpace(request.Audience) ? "Guest" : request.Audience.Trim(),
                Status = SupportChatStatuses.Waiting,
                Priority = SupportPriorities.Normal,
                Channel = string.IsNullOrWhiteSpace(request.Channel) ? SupportChannels.Website : request.Channel.Trim(),
                CurrentPageUrl = Trunc(request.CurrentPageUrl, 500),
                ExternalThreadId = Trunc(request.ExternalThreadId ?? ExtractExternalId(request.GuestToken), 80)
            });

            var sys = new SupportChatMessage
            {
                ChatId = created.Id,
                SenderType = SupportChatSenders.System,
                Body = welcome,
                MessageType = SupportMessageTypes.SystemEvent
            };
            await _repo.AddMessageAsync(sys);
            await _repo.TouchAsync(created.Id, Trunc(welcome, 200), SupportChatStatuses.Waiting,
                incrementAdminUnread: true, incrementUserUnread: false, clearAdminUnread: false, clearUserUnread: true, assignedAdminUserId: null);
            if (_realtime != null)
            {
                await _realtime.NewMessageAsync(created.Id, created.SiteId, sys);
                await _realtime.ConversationUpdatedAsync(created.Id, created.SiteId, SupportChatStatuses.Waiting);
            }
            await SafeNotifyAsync(created.SiteId, created.AssignedAdminUserId, created.Id, SupportNotifyKinds.NewMessage,
                "گفتگوی جدید", Trunc(created.VisitorName ?? created.UserDisplayName ?? "مهمان", 80) + " منتظر پاسخ است.");
            return await _repo.GetAsync(created.Id);
        }

        public async Task<SupportChatMessage> SendFromUserAsync(Guid chatId, int? userId, string body, string messageType = null, string attachmentUrl = null, string attachmentName = null, int? attachmentSize = null)
        {
            var text = (body ?? "").Trim();
            if (string.IsNullOrWhiteSpace(text) && string.IsNullOrWhiteSpace(attachmentUrl)) return null;
            var chat = await _repo.GetAsync(chatId) ?? throw new InvalidOperationException("گفتگو یافت نشد.");
            var type = string.IsNullOrWhiteSpace(messageType)
                ? (IsImage(attachmentName) ? SupportMessageTypes.Image : string.IsNullOrWhiteSpace(attachmentUrl) ? SupportMessageTypes.Text : SupportMessageTypes.File)
                : messageType;
            var msg = new SupportChatMessage
            {
                ChatId = chatId,
                SenderType = SupportChatSenders.User,
                SenderUserId = userId,
                Body = string.IsNullOrWhiteSpace(text) ? (attachmentName ?? "پیوست") : text,
                MessageType = type,
                AttachmentUrl = attachmentUrl,
                AttachmentName = attachmentName,
                AttachmentSize = attachmentSize
            };
            await _repo.AddMessageAsync(msg);
            var status = chat.Status == SupportChatStatuses.Closed ? SupportChatStatuses.Waiting : chat.Status;
            if (status != SupportChatStatuses.Active) status = SupportChatStatuses.Waiting;
            await _repo.TouchAsync(chatId, Trunc(msg.Body, 200), status,
                incrementAdminUnread: true, incrementUserUnread: false, clearAdminUnread: false, clearUserUnread: true, assignedAdminUserId: null);
            if (_realtime != null)
            {
                await _realtime.NewMessageAsync(chatId, chat.SiteId, msg);
                await _realtime.ConversationUpdatedAsync(chatId, chat.SiteId, status);
            }
            await SafeNotifyAsync(chat.SiteId, chat.AssignedAdminUserId, chatId, SupportNotifyKinds.NewMessage,
                "پیام جدید پشتیبانی", Trunc(msg.Body, 120));
            if (_autoReply != null)
            {
                try { await _autoReply.TryAfterUserMessageAsync(await _repo.GetAsync(chatId), msg); } catch { }
            }
            return msg;
        }

        public async Task<SupportChatMessage> SendFromAdminAsync(Guid chatId, int adminUserId, string body, string messageType = null, string attachmentUrl = null, string attachmentName = null, int? attachmentSize = null)
        {
            var text = (body ?? "").Trim();
            if (string.IsNullOrWhiteSpace(text) && string.IsNullOrWhiteSpace(attachmentUrl)) return null;
            var chat = await _repo.GetAsync(chatId) ?? throw new InvalidOperationException("گفتگو یافت نشد.");
            var type = string.IsNullOrWhiteSpace(messageType)
                ? (IsImage(attachmentName) ? SupportMessageTypes.Image : string.IsNullOrWhiteSpace(attachmentUrl) ? SupportMessageTypes.Text : SupportMessageTypes.File)
                : messageType;
            var msg = new SupportChatMessage
            {
                ChatId = chatId,
                SenderType = SupportChatSenders.Admin,
                SenderUserId = adminUserId,
                Body = string.IsNullOrWhiteSpace(text) ? (attachmentName ?? "پیوست") : text,
                MessageType = type,
                AttachmentUrl = attachmentUrl,
                AttachmentName = attachmentName,
                AttachmentSize = attachmentSize
            };
            await _repo.AddMessageAsync(msg);
            await _repo.TouchAsync(chatId, Trunc(msg.Body, 200), SupportChatStatuses.Active,
                incrementAdminUnread: false, incrementUserUnread: true, clearAdminUnread: true, clearUserUnread: false, assignedAdminUserId: adminUserId);
            if (_analytics != null)
            {
                try { await _analytics.MarkFirstResponseAsync(chatId); } catch { }
            }
            if (_realtime != null)
            {
                await _realtime.NewMessageAsync(chatId, chat.SiteId, msg);
                await _realtime.ConversationUpdatedAsync(chatId, chat.SiteId, SupportChatStatuses.Active);
            }
            await SafeOutboundAsync(chat, msg.Body);
            return msg;
        }

        public Task<List<SupportChatMessage>> GetMessagesAsync(Guid chatId, long afterId = 0) =>
            _repo.ListMessagesAsync(chatId, afterId);

        public Task<SupportChatThread> GetAsync(Guid id) => _repo.GetAsync(id);

        public Task<List<SupportChatThread>> ListInboxAsync(int? siteId, string status = null) =>
            _repo.ListForAdminAsync(siteId, status);

        public async Task CloseAsync(Guid chatId)
        {
            var chat = await _repo.GetAsync(chatId);
            await _repo.CloseAsync(chatId);
            if (_realtime != null && chat != null)
                await _realtime.ConversationUpdatedAsync(chatId, chat.SiteId, SupportChatStatuses.Closed);
        }

        public async Task MarkReadByUserAsync(Guid chatId)
        {
            await _repo.TouchAsync(chatId, null, null, false, false, false, true, null);
            await _repo.MarkMessagesReadAsync(chatId, "User");
            if (_realtime != null)
                await _realtime.MessageReadAsync(chatId, "User");
        }

        public async Task MarkReadByAdminAsync(Guid chatId, int adminUserId)
        {
            await _repo.TouchAsync(chatId, null, SupportChatStatuses.Active, false, false, true, false, adminUserId);
            await _repo.MarkMessagesReadAsync(chatId, "Admin");
            if (_realtime != null)
                await _realtime.MessageReadAsync(chatId, "Admin");
        }

        public Task NotifyTypingAsync(Guid chatId, string senderSide, bool isTyping) =>
            _realtime == null ? Task.CompletedTask : _realtime.TypingAsync(chatId, senderSide, isTyping);

        public Task UpdateCurrentPageAsync(Guid chatId, string pageUrl) =>
            _repo.UpdatePageAsync(chatId, pageUrl);

        public Task<SupportSettings> GetSettingsAsync(int siteId) => _repo.GetSettingsAsync(siteId);

        public Task SaveSettingsAsync(SupportSettings settings) => _repo.UpsertSettingsAsync(settings);

        async Task SafeNotifyAsync(int? siteId, int? userId, Guid? chatId, string kind, string title, string body)
        {
            if (_assist == null) return;
            try
            {
                await _assist.NotifyOperatorsAsync(siteId, userId, chatId, kind, title, body);
            }
            catch
            {
                /* جدول نوتیف فاز ۴ ممکن است هنوز اجرا نشده باشد */
            }
        }

        async Task SafeOutboundAsync(SupportChatThread chat, string body)
        {
            if (_outbound == null || chat == null || string.IsNullOrWhiteSpace(body)) return;
            if (!SupportOmnichannelService.TryParseExternal(chat, out var provider, out var extId)) return;
            try { await _outbound.SendToExternalAsync(provider, extId, body); } catch { }
        }

        static string ExtractExternalId(string guestToken)
        {
            if (string.IsNullOrWhiteSpace(guestToken)) return null;
            var i = guestToken.IndexOf(':');
            return i > 0 ? guestToken.Substring(i + 1) : guestToken;
        }

        static bool IsImage(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            var n = name.ToLowerInvariant();
            return n.EndsWith(".jpg") || n.EndsWith(".jpeg") || n.EndsWith(".png") || n.EndsWith(".gif") || n.EndsWith(".webp");
        }

        static string Trunc(string s, int n)
        {
            if (string.IsNullOrEmpty(s)) return s;
            s = s.Trim();
            return s.Length <= n ? s : s.Substring(0, n);
        }
    }
}
