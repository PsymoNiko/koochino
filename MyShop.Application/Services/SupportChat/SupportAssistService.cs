using MyShop.Application.Interfaces.SupportChat;
using MyShop.Core.Models.SupportChat;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Services.SupportChat
{
    public class SupportAssistService : ISupportAssistService
    {
        private readonly ISupportAssistRepository _repo;
        private readonly ISupportChatRepository _chats;
        private readonly ISupportChatRealtimePublisher _realtime;

        public SupportAssistService(
            ISupportAssistRepository repo,
            ISupportChatRepository chats,
            ISupportChatRealtimePublisher realtime)
        {
            _repo = repo;
            _chats = chats;
            _realtime = realtime;
        }

        public Task<List<SupportCannedReply>> ListCannedAsync(int? siteId, bool activeOnly = false) =>
            _repo.ListCannedAsync(siteId, activeOnly);

        public Task<int> SaveCannedAsync(SupportCannedReply reply)
        {
            if (reply == null || string.IsNullOrWhiteSpace(reply.Title) || string.IsNullOrWhiteSpace(reply.Body))
                throw new InvalidOperationException("عنوان و متن پاسخ آماده الزامی است.");
            reply.Title = reply.Title.Trim();
            reply.Body = reply.Body.Trim();
            return _repo.UpsertCannedAsync(reply);
        }

        public Task DeleteCannedAsync(int id) => _repo.DeleteCannedAsync(id);

        public async Task<string> ResolveSlashCommandAsync(int? siteId, string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return input;
            var text = input.Trim();
            if (!text.StartsWith("/")) return input;
            var token = text.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries)[0];
            var canned = await _repo.GetCannedByShortcutAsync(siteId, token);
            if (canned == null) return input;
            var rest = text.Length > token.Length ? text.Substring(token.Length).Trim() : "";
            return string.IsNullOrEmpty(rest) ? canned.Body : canned.Body + "\n" + rest;
        }

        public Task<List<SupportOperatorNotification>> ListNotificationsAsync(int? siteId, int? userId, bool unreadOnly = false) =>
            _repo.ListNotificationsAsync(siteId, userId, unreadOnly);

        public Task MarkNotificationReadAsync(long id) => _repo.MarkNotificationReadAsync(id);
        public Task MarkAllNotificationsReadAsync(int? siteId, int? userId) => _repo.MarkAllNotificationsReadAsync(siteId, userId);
        public Task<int> CountUnreadAsync(int? siteId, int? userId) => _repo.CountUnreadNotificationsAsync(siteId, userId);

        public async Task NotifyOperatorsAsync(int? siteId, int? userId, Guid? chatId, string kind, string title, string body)
        {
            await _repo.AddNotificationAsync(new SupportOperatorNotification
            {
                SiteId = siteId,
                UserId = userId,
                ChatId = chatId,
                Kind = kind ?? SupportNotifyKinds.NewMessage,
                Title = Trunc(title, 160),
                Body = Trunc(body, 500)
            });
            await _realtime.OperatorNotifyAsync(siteId, new SupportOperatorNotification
            {
                SiteId = siteId,
                UserId = userId,
                ChatId = chatId,
                Kind = kind,
                Title = title,
                Body = body,
                CreatedAt = DateTime.Now
            });
        }

        public async Task<List<SupportWaitAlertItem>> RaiseWaitAlertsAsync(int siteId)
        {
            var settings = await _chats.GetSettingsAsync(siteId);
            if (settings == null || !settings.WaitAlertEnabled)
                return new List<SupportWaitAlertItem>();

            var overdue = await _repo.FindWaitingOverdueAsync(siteId, settings.WaitAlertMinutes);
            foreach (var item in overdue)
            {
                await _repo.AddNotificationAsync(new SupportOperatorNotification
                {
                    SiteId = siteId,
                    ChatId = item.ChatId,
                    Kind = SupportNotifyKinds.WaitAlert,
                    Title = "انتظار طولانی",
                    Body = $"{item.DisplayName} حدود {item.WaitMinutes} دقیقه منتظر است."
                });
                await _repo.MarkWaitAlertedAsync(item.ChatId);
                await _realtime.OperatorNotifyAsync(siteId, new SupportOperatorNotification
                {
                    SiteId = siteId,
                    ChatId = item.ChatId,
                    Kind = SupportNotifyKinds.WaitAlert,
                    Title = "انتظار طولانی",
                    Body = $"{item.DisplayName} حدود {item.WaitMinutes} دقیقه منتظر است.",
                    CreatedAt = DateTime.Now
                });
            }
            return overdue;
        }

        static string Trunc(string s, int n)
        {
            if (string.IsNullOrEmpty(s)) return s;
            s = s.Trim();
            return s.Length <= n ? s : s.Substring(0, n);
        }
    }
}
