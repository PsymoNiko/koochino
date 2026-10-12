using Dapper;
using MyShop.Application.Interfaces.SupportChat;
using MyShop.Core.Interfaces;
using MyShop.Core.Models.SupportChat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Infrastructure.Repositories.SupportChat
{
    public class SupportAssistRepository : ISupportAssistRepository
    {
        private readonly IDbConnectionFactory _factory;
        public SupportAssistRepository(IDbConnectionFactory factory) => _factory = factory;

        public async Task<List<SupportCannedReply>> ListCannedAsync(int? siteId, bool activeOnly = false)
        {
            using var conn = _factory.CreateConnection();
            var rows = await conn.QueryAsync<SupportCannedReply>(@"
SELECT * FROM koch.SupportCannedReplies
WHERE (@SiteId IS NULL OR @SiteId=0 OR SiteId=@SiteId OR SiteId IS NULL)
  AND (@ActiveOnly=0 OR IsActive=1)
ORDER BY SortOrder, Title", new { SiteId = siteId, ActiveOnly = activeOnly ? 1 : 0 });
            return rows?.ToList() ?? new List<SupportCannedReply>();
        }

        public async Task<SupportCannedReply> GetCannedByIdAsync(int id)
        {
            using var conn = _factory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<SupportCannedReply>(
                "SELECT * FROM koch.SupportCannedReplies WHERE Id=@Id", new { Id = id });
        }

        public async Task<SupportCannedReply> GetCannedByShortcutAsync(int? siteId, string shortcut)
        {
            using var conn = _factory.CreateConnection();
            var key = (shortcut ?? "").Trim().TrimStart('/').ToLowerInvariant();
            return await conn.QueryFirstOrDefaultAsync<SupportCannedReply>(@"
SELECT TOP 1 * FROM koch.SupportCannedReplies
WHERE IsActive=1 AND LOWER(LTRIM(RTRIM(Shortcut)))=@Key
  AND (@SiteId IS NULL OR SiteId=@SiteId OR SiteId IS NULL)
ORDER BY CASE WHEN SiteId=@SiteId THEN 0 ELSE 1 END, SortOrder",
                new { Key = key, SiteId = siteId });
        }

        public async Task<int> UpsertCannedAsync(SupportCannedReply reply)
        {
            using var conn = _factory.CreateConnection();
            reply.Shortcut = NormalizeShortcut(reply.Shortcut);
            if (reply.Id <= 0)
            {
                return await conn.ExecuteScalarAsync<int>(@"
INSERT INTO koch.SupportCannedReplies
(SiteId, Title, Shortcut, Body, Category, SortOrder, IsActive, CreatedByUserId, UpdatedAt)
OUTPUT INSERTED.Id
VALUES (@SiteId, @Title, @Shortcut, @Body, @Category, @SortOrder, @IsActive, @CreatedByUserId, SYSDATETIME())", reply);
            }
            await conn.ExecuteAsync(@"
UPDATE koch.SupportCannedReplies SET
 Title=@Title, Shortcut=@Shortcut, Body=@Body, Category=@Category, SortOrder=@SortOrder,
 IsActive=@IsActive, UpdatedAt=SYSDATETIME()
WHERE Id=@Id", reply);
            return reply.Id;
        }

        public async Task DeleteCannedAsync(int id)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync("DELETE FROM koch.SupportCannedReplies WHERE Id=@Id", new { Id = id });
        }

        public async Task AddNotificationAsync(SupportOperatorNotification n)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(@"
INSERT INTO koch.SupportOperatorNotifications (SiteId, UserId, ChatId, Kind, Title, Body)
VALUES (@SiteId, @UserId, @ChatId, @Kind, @Title, @Body)", n);
        }

        public async Task<List<SupportOperatorNotification>> ListNotificationsAsync(int? siteId, int? userId, bool unreadOnly = false, int take = 30)
        {
            using var conn = _factory.CreateConnection();
            var rows = await conn.QueryAsync<SupportOperatorNotification>(@"
SELECT TOP (@Take) * FROM koch.SupportOperatorNotifications
WHERE (@SiteId IS NULL OR @SiteId=0 OR SiteId=@SiteId OR SiteId IS NULL)
  AND (UserId IS NULL OR UserId=@UserId OR @UserId IS NULL)
  AND (@UnreadOnly=0 OR IsRead=0)
ORDER BY Id DESC",
                new { SiteId = siteId, UserId = userId, UnreadOnly = unreadOnly ? 1 : 0, Take = take <= 0 ? 30 : take });
            return rows?.ToList() ?? new List<SupportOperatorNotification>();
        }

        public async Task MarkNotificationReadAsync(long id)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync("UPDATE koch.SupportOperatorNotifications SET IsRead=1 WHERE Id=@Id", new { Id = id });
        }

        public async Task MarkAllNotificationsReadAsync(int? siteId, int? userId)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(@"
UPDATE koch.SupportOperatorNotifications SET IsRead=1
WHERE IsRead=0
  AND (@SiteId IS NULL OR SiteId=@SiteId OR SiteId IS NULL)
  AND (UserId IS NULL OR UserId=@UserId OR @UserId IS NULL)",
                new { SiteId = siteId, UserId = userId });
        }

        public async Task<int> CountUnreadNotificationsAsync(int? siteId, int? userId)
        {
            using var conn = _factory.CreateConnection();
            return await conn.ExecuteScalarAsync<int>(@"
SELECT COUNT(1) FROM koch.SupportOperatorNotifications
WHERE IsRead=0
  AND (@SiteId IS NULL OR SiteId=@SiteId OR SiteId IS NULL)
  AND (UserId IS NULL OR UserId=@UserId OR @UserId IS NULL)",
                new { SiteId = siteId, UserId = userId });
        }

        public async Task<List<SupportWaitAlertItem>> FindWaitingOverdueAsync(int siteId, int waitMinutes)
        {
            using var conn = _factory.CreateConnection();
            var mins = waitMinutes <= 0 ? 5 : waitMinutes;
            var rows = await conn.QueryAsync<SupportWaitAlertItem>(@"
SELECT c.Id AS ChatId, c.SiteId,
       CASE
         WHEN NULLIF(LTRIM(RTRIM(ISNULL(u.FirstName,N'') + N' ' + ISNULL(u.LastName,N''))), N'') IS NOT NULL
           THEN LTRIM(RTRIM(ISNULL(u.FirstName,N'') + N' ' + ISNULL(u.LastName,N'')))
         WHEN NULLIF(LTRIM(RTRIM(ISNULL(c.VisitorName,N''))), N'') IS NOT NULL THEN c.VisitorName
         ELSE N'مهمان'
       END AS DisplayName,
       DATEDIFF(MINUTE, c.LastMessageAt, SYSDATETIME()) AS WaitMinutes,
       c.LastMessageAt
FROM koch.SupportChats c
LEFT JOIN koch.Users u ON u.Id=c.UserId
WHERE c.SiteId=@SiteId
  AND c.Status=N'Waiting'
  AND c.LastMessageAt IS NOT NULL
  AND DATEDIFF(MINUTE, c.LastMessageAt, SYSDATETIME()) >= @Mins
  AND (c.LastWaitAlertAt IS NULL OR c.LastWaitAlertAt < c.LastMessageAt)",
                new { SiteId = siteId, Mins = mins });
            return rows?.ToList() ?? new List<SupportWaitAlertItem>();
        }

        public async Task MarkWaitAlertedAsync(Guid chatId)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(
                "UPDATE koch.SupportChats SET LastWaitAlertAt=SYSDATETIME() WHERE Id=@Id",
                new { Id = chatId });
        }

        static string NormalizeShortcut(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            s = s.Trim().TrimStart('/').ToLowerInvariant();
            return s.Length > 40 ? s.Substring(0, 40) : s;
        }
    }
}
