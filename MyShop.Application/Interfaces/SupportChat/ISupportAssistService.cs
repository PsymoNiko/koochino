using MyShop.Core.Models.SupportChat;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Interfaces.SupportChat
{
    public interface ISupportAssistRepository
    {
        Task<List<SupportCannedReply>> ListCannedAsync(int? siteId, bool activeOnly = false);
        Task<SupportCannedReply> GetCannedByIdAsync(int id);
        Task<SupportCannedReply> GetCannedByShortcutAsync(int? siteId, string shortcut);
        Task<int> UpsertCannedAsync(SupportCannedReply reply);
        Task DeleteCannedAsync(int id);

        Task AddNotificationAsync(SupportOperatorNotification n);
        Task<List<SupportOperatorNotification>> ListNotificationsAsync(int? siteId, int? userId, bool unreadOnly = false, int take = 30);
        Task MarkNotificationReadAsync(long id);
        Task MarkAllNotificationsReadAsync(int? siteId, int? userId);
        Task<int> CountUnreadNotificationsAsync(int? siteId, int? userId);

        Task<List<SupportWaitAlertItem>> FindWaitingOverdueAsync(int siteId, int waitMinutes);
        Task MarkWaitAlertedAsync(Guid chatId);
    }

    public interface ISupportAssistService
    {
        Task<List<SupportCannedReply>> ListCannedAsync(int? siteId, bool activeOnly = false);
        Task<int> SaveCannedAsync(SupportCannedReply reply);
        Task DeleteCannedAsync(int id);
        Task<string> ResolveSlashCommandAsync(int? siteId, string input);

        Task<List<SupportOperatorNotification>> ListNotificationsAsync(int? siteId, int? userId, bool unreadOnly = false);
        Task MarkNotificationReadAsync(long id);
        Task MarkAllNotificationsReadAsync(int? siteId, int? userId);
        Task<int> CountUnreadAsync(int? siteId, int? userId);
        Task NotifyOperatorsAsync(int? siteId, int? userId, Guid? chatId, string kind, string title, string body);

        /// <summary>گفتگوهای Waiting که از آستانه گذشته‌اند را هشدار می‌دهد و اعلان می‌سازد.</summary>
        Task<List<SupportWaitAlertItem>> RaiseWaitAlertsAsync(int siteId);
    }
}
