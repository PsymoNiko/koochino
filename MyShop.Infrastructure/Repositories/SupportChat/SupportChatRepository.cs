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
    public class SupportChatRepository : ISupportChatRepository
    {
        private const string ThreadSelect = @"
SELECT c.Id, c.SiteId, c.UserId, c.GuestToken, c.VisitorName, c.VisitorPhone, c.Audience, c.Status,
       ISNULL(c.Priority, N'Normal') AS Priority, ISNULL(c.Channel, N'Website') AS Channel,
       c.AssignedAdminUserId, c.TeamId, t.Name AS TeamName,
       c.LastMessageAt, c.LastMessagePreview, c.UnreadByAdmin, c.UnreadByUser, c.CreatedAt,
       ISNULL(c.UpdatedAt, c.CreatedAt) AS UpdatedAt, c.ClosedAt, c.CurrentPageUrl, c.ExternalThreadId,
       LTRIM(RTRIM(ISNULL(u.FirstName,N'') + N' ' + ISNULL(u.LastName,N''))) AS UserDisplayName,
       u.PhoneNumber AS UserPhone,
       LTRIM(RTRIM(ISNULL(a.FirstName,N'') + N' ' + ISNULL(a.LastName,N''))) AS AssignedAdminName
FROM koch.SupportChats c
LEFT JOIN koch.Users u ON u.Id = c.UserId
LEFT JOIN koch.Users a ON a.Id = c.AssignedAdminUserId
LEFT JOIN koch.SupportTeams t ON t.Id = c.TeamId";

        private readonly IDbConnectionFactory _factory;
        public SupportChatRepository(IDbConnectionFactory factory) => _factory = factory;

        public async Task<SupportChatThread> GetAsync(Guid id)
        {
            using var conn = _factory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<SupportChatThread>(
                ThreadSelect + " WHERE c.Id=@Id", new { Id = id });
        }

        public async Task<SupportChatThread> GetOpenAsync(int? siteId, int? userId, string guestToken)
        {
            using var conn = _factory.CreateConnection();
            if (userId.HasValue && userId.Value > 0)
            {
                return await conn.QueryFirstOrDefaultAsync<SupportChatThread>(
                    ThreadSelect + @"
WHERE c.UserId=@UserId AND (@SiteId IS NULL OR c.SiteId=@SiteId)
  AND c.Status IN (N'Waiting', N'Active', N'Pending')
ORDER BY c.LastMessageAt DESC",
                    new { UserId = userId.Value, SiteId = siteId });
            }
            if (string.IsNullOrWhiteSpace(guestToken)) return null;
            return await conn.QueryFirstOrDefaultAsync<SupportChatThread>(
                ThreadSelect + @"
WHERE c.GuestToken=@GuestToken AND (@SiteId IS NULL OR c.SiteId=@SiteId)
  AND c.Status IN (N'Waiting', N'Active', N'Pending')
ORDER BY c.LastMessageAt DESC",
                new { GuestToken = guestToken, SiteId = siteId });
        }

        public async Task<SupportChatThread> CreateAsync(SupportChatThread row)
        {
            using var conn = _factory.CreateConnection();
            var id = await conn.ExecuteScalarAsync<Guid>(@"
INSERT INTO koch.SupportChats (SiteId, UserId, GuestToken, VisitorName, VisitorPhone, Audience, Status, Priority, Channel, CurrentPageUrl, ExternalThreadId, UpdatedAt)
OUTPUT INSERTED.Id
VALUES (@SiteId, @UserId, @GuestToken, @VisitorName, @VisitorPhone, @Audience, @Status, @Priority, @Channel, @CurrentPageUrl, @ExternalThreadId, SYSDATETIME())", row);
            return await GetAsync(id);
        }

        public async Task<List<SupportChatThread>> ListForAdminAsync(int? siteId, string status, int take = 80)
        {
            using var conn = _factory.CreateConnection();
            var rows = await conn.QueryAsync<SupportChatThread>(
                ThreadSelect + @"
WHERE (@SiteId IS NULL OR @SiteId=0 OR c.SiteId=@SiteId)
  AND (@Status IS NULL OR @Status=N'' OR c.Status=@Status)
ORDER BY c.LastMessageAt DESC
OFFSET 0 ROWS FETCH NEXT @Take ROWS ONLY",
                new { SiteId = siteId, Status = status, Take = take <= 0 ? 80 : take });
            return rows?.ToList() ?? new List<SupportChatThread>();
        }

        public async Task AddMessageAsync(SupportChatMessage msg)
        {
            using var conn = _factory.CreateConnection();
            var id = await conn.ExecuteScalarAsync<long>(@"
INSERT INTO koch.SupportChatMessages (ChatId, SenderType, SenderUserId, Body, MessageType, AttachmentUrl, AttachmentName, AttachmentSize)
OUTPUT INSERTED.Id
VALUES (@ChatId, @SenderType, @SenderUserId, @Body, ISNULL(@MessageType, N'Text'), @AttachmentUrl, @AttachmentName, @AttachmentSize)", msg);
            msg.Id = id;
        }

        public async Task<List<SupportChatMessage>> ListMessagesAsync(Guid chatId, long afterId = 0)
        {
            using var conn = _factory.CreateConnection();
            var rows = await conn.QueryAsync<SupportChatMessage>(@"
SELECT Id, ChatId, SenderType, SenderUserId, Body,
       ISNULL(MessageType, N'Text') AS MessageType, AttachmentUrl, AttachmentName, AttachmentSize, CreatedAt, ReadAt
FROM koch.SupportChatMessages
WHERE ChatId=@ChatId AND Id>@AfterId
ORDER BY Id",
                new { ChatId = chatId, AfterId = afterId });
            return rows?.ToList() ?? new List<SupportChatMessage>();
        }

        public async Task TouchAsync(Guid chatId, string preview, string status, bool incrementAdminUnread, bool incrementUserUnread, bool clearAdminUnread, bool clearUserUnread, int? assignedAdminUserId, string currentPageUrl = null)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(@"
UPDATE koch.SupportChats SET
    LastMessageAt = CASE WHEN @Preview IS NULL THEN LastMessageAt ELSE SYSDATETIME() END,
    LastMessagePreview = CASE WHEN @Preview IS NULL THEN LastMessagePreview ELSE @Preview END,
    Status = ISNULL(@Status, Status),
    AssignedAdminUserId = CASE WHEN @AssignedAdminUserId IS NULL THEN AssignedAdminUserId ELSE @AssignedAdminUserId END,
    UnreadByAdmin = CASE WHEN @ClearAdminUnread=1 THEN 0 WHEN @IncAdmin=1 THEN UnreadByAdmin+1 ELSE UnreadByAdmin END,
    UnreadByUser = CASE WHEN @ClearUserUnread=1 THEN 0 WHEN @IncUser=1 THEN UnreadByUser+1 ELSE UnreadByUser END,
    CurrentPageUrl = CASE WHEN @CurrentPageUrl IS NULL THEN CurrentPageUrl ELSE @CurrentPageUrl END,
    UpdatedAt = SYSDATETIME(),
    ClosedAt = CASE WHEN @Status=N'Closed' THEN ISNULL(ClosedAt, SYSDATETIME()) WHEN @Status IS NOT NULL AND @Status<>N'Closed' THEN NULL ELSE ClosedAt END
WHERE Id=@Id",
                new
                {
                    Id = chatId,
                    Preview = preview,
                    Status = status,
                    AssignedAdminUserId = assignedAdminUserId,
                    IncAdmin = incrementAdminUnread ? 1 : 0,
                    IncUser = incrementUserUnread ? 1 : 0,
                    ClearAdminUnread = clearAdminUnread ? 1 : 0,
                    ClearUserUnread = clearUserUnread ? 1 : 0,
                    CurrentPageUrl = currentPageUrl
                });
        }

        public async Task CloseAsync(Guid chatId)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(@"
UPDATE koch.SupportChats
SET Status=N'Closed', ClosedAt=SYSDATETIME(), UpdatedAt=SYSDATETIME(), LastMessageAt=SYSDATETIME()
WHERE Id=@Id",
                new { Id = chatId });
        }

        public async Task MarkMessagesReadAsync(Guid chatId, string readerSide)
        {
            using var conn = _factory.CreateConnection();
            if (string.Equals(readerSide, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                await conn.ExecuteAsync(@"
UPDATE koch.SupportChatMessages
SET ReadAt = SYSDATETIME()
WHERE ChatId=@ChatId AND SenderType=N'User' AND ReadAt IS NULL",
                    new { ChatId = chatId });
            }
            else
            {
                await conn.ExecuteAsync(@"
UPDATE koch.SupportChatMessages
SET ReadAt = SYSDATETIME()
WHERE ChatId=@ChatId AND SenderType=N'Admin' AND ReadAt IS NULL",
                    new { ChatId = chatId });
            }
        }

        public async Task UpdatePageAsync(Guid chatId, string pageUrl)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(
                "UPDATE koch.SupportChats SET CurrentPageUrl=@Url, UpdatedAt=SYSDATETIME() WHERE Id=@Id",
                new { Id = chatId, Url = Trunc(pageUrl, 500) });
        }

        public async Task<SupportSettings> GetSettingsAsync(int siteId)
        {
            using var conn = _factory.CreateConnection();
            var row = await conn.QueryFirstOrDefaultAsync<SupportSettings>(
                "SELECT * FROM koch.SupportSettings WHERE SiteId=@SiteId", new { SiteId = siteId });
            if (row != null) return row;
            return new SupportSettings { SiteId = siteId };
        }

        public async Task UpsertSettingsAsync(SupportSettings settings)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(@"
MERGE koch.SupportSettings AS t
USING (SELECT @SiteId AS SiteId) AS s ON t.SiteId=s.SiteId
WHEN MATCHED THEN UPDATE SET
    IsChatEnabled=@IsChatEnabled, WelcomeMessage=@WelcomeMessage, OfflineMessage=@OfflineMessage,
    WidgetPosition=@WidgetPosition, WidgetColor=@WidgetColor, MaxUploadMb=@MaxUploadMb,
    AllowedExtensions=@AllowedExtensions,
    TimeZoneId=ISNULL(@TimeZoneId, TimeZoneId),
    UseBusinessHours=@UseBusinessHours,
    WaitAlertEnabled=@WaitAlertEnabled,
    WaitAlertMinutes=@WaitAlertMinutes,
    SoundNotifyEnabled=@SoundNotifyEnabled,
    BrowserNotifyEnabled=@BrowserNotifyEnabled,
    CsatEnabled=@CsatEnabled,
    CsatPrompt=ISNULL(@CsatPrompt, CsatPrompt),
    AutoReplyEnabled=@AutoReplyEnabled,
    AutoReplyMinScore=@AutoReplyMinScore,
    AutoReplyMaxPerChat=@AutoReplyMaxPerChat,
    AutoReplyHandoffHint=ISNULL(@AutoReplyHandoffHint, AutoReplyHandoffHint),
    AutoReplyUseAi=@AutoReplyUseAi,
    AiEnabled=@AiEnabled,
    AiMinRetrievalScore=@AiMinRetrievalScore,
    AiMaxMessagesPerUserPerDay=@AiMaxMessagesPerUserPerDay,
    AiMaxTokensPerSitePerDay=@AiMaxTokensPerSitePerDay,
    AiMaxInputChars=@AiMaxInputChars,
    AiHistoryTurns=@AiHistoryTurns,
    AiTemperature=@AiTemperature,
    AiOutOfScopeMessage=ISNULL(@AiOutOfScopeMessage, AiOutOfScopeMessage),
    AiEmbeddingsEnabled=@AiEmbeddingsEnabled,
    AiLearnFromAdminChat=@AiLearnFromAdminChat,
    OmnichannelEnabled=@OmnichannelEnabled,
    OmnichannelInterceptFreeText=@OmnichannelInterceptFreeText,
    UpdatedAt=SYSDATETIME()
WHEN NOT MATCHED THEN INSERT
    (SiteId, IsChatEnabled, WelcomeMessage, OfflineMessage, WidgetPosition, WidgetColor, MaxUploadMb, AllowedExtensions, TimeZoneId, UseBusinessHours, WaitAlertEnabled, WaitAlertMinutes, SoundNotifyEnabled, BrowserNotifyEnabled, CsatEnabled, CsatPrompt, AutoReplyEnabled, AutoReplyMinScore, AutoReplyMaxPerChat, AutoReplyHandoffHint, AutoReplyUseAi, AiEnabled, AiMinRetrievalScore, AiMaxMessagesPerUserPerDay, AiMaxTokensPerSitePerDay, AiMaxInputChars, AiHistoryTurns, AiTemperature, AiOutOfScopeMessage, AiEmbeddingsEnabled, AiLearnFromAdminChat, OmnichannelEnabled, OmnichannelInterceptFreeText, UpdatedAt)
VALUES
    (@SiteId, @IsChatEnabled, @WelcomeMessage, @OfflineMessage, @WidgetPosition, @WidgetColor, @MaxUploadMb, @AllowedExtensions, ISNULL(@TimeZoneId, N'Iran Standard Time'), @UseBusinessHours, @WaitAlertEnabled, @WaitAlertMinutes, @SoundNotifyEnabled, @BrowserNotifyEnabled, @CsatEnabled, ISNULL(@CsatPrompt, N'از پشتیبانی راضی بودید؟ امتیاز دهید.'), @AutoReplyEnabled, @AutoReplyMinScore, @AutoReplyMaxPerChat, ISNULL(@AutoReplyHandoffHint, N'اگر این پاسخ کافی نبود، همین‌جا بنویسید تا پشتیبان انسانی ادامه دهد.'), @AutoReplyUseAi, @AiEnabled, @AiMinRetrievalScore, @AiMaxMessagesPerUserPerDay, @AiMaxTokensPerSitePerDay, @AiMaxInputChars, @AiHistoryTurns, @AiTemperature, ISNULL(@AiOutOfScopeMessage, N'این سوال خارج از حوزه پشتیبانی این سایت است.'), @AiEmbeddingsEnabled, @AiLearnFromAdminChat, @OmnichannelEnabled, @OmnichannelInterceptFreeText, SYSDATETIME());",
                settings);
        }

        static string Trunc(string s, int n)
        {
            if (string.IsNullOrEmpty(s)) return s;
            s = s.Trim();
            return s.Length <= n ? s : s.Substring(0, n);
        }
    }
}
