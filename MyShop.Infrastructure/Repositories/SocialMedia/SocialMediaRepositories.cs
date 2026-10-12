using Dapper;
using MyShop.Application.Interfaces.SocialMedia;
using MyShop.Core.Interfaces;
using MyShop.Core.Models.SocialMedia;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Infrastructure.Repositories.SocialMedia
{
    public class SocialAccountRepository : ISocialAccountRepository
    {
        private readonly IDbConnectionFactory _factory;
        public SocialAccountRepository(IDbConnectionFactory factory) => _factory = factory;

        public async Task<List<SocialAccount>> ListAsync(int? siteId = null, string provider = null, bool activeOnly = false)
        {
            try
            {
                using var conn = _factory.CreateConnection();
                var rows = await conn.QueryAsync<SocialAccount>(@"
                    SELECT a.*, s.Code AS SiteCode, s.Name AS SiteName
                    FROM koch.SocialAccounts a
                    LEFT JOIN koch.Sites s ON s.Id = a.SiteId
                    WHERE (@SiteId IS NULL OR a.SiteId = @SiteId)
                      AND (@Provider IS NULL OR a.Provider = @Provider)
                      AND (@ActiveOnly = 0 OR a.IsActive = 1)
                    ORDER BY a.SiteId, a.Provider, a.DisplayName",
                    new
                    {
                        SiteId = siteId,
                        Provider = string.IsNullOrWhiteSpace(provider) ? null : provider.Trim(),
                        ActiveOnly = activeOnly ? 1 : 0
                    });
                return rows?.ToList() ?? new List<SocialAccount>();
            }
            catch
            {
                return new List<SocialAccount>();
            }
        }

        public async Task<SocialAccount> GetByIdAsync(int id)
        {
            try
            {
                using var conn = _factory.CreateConnection();
                return await conn.QueryFirstOrDefaultAsync<SocialAccount>(@"
                    SELECT a.*, s.Code AS SiteCode, s.Name AS SiteName
                    FROM koch.SocialAccounts a
                    LEFT JOIN koch.Sites s ON s.Id = a.SiteId
                    WHERE a.Id = @Id", new { Id = id });
            }
            catch { return null; }
        }

        public async Task<int> UpsertAsync(SocialAccount account)
        {
            using var conn = _factory.CreateConnection();
            if (account.Id > 0)
            {
                await conn.ExecuteAsync(@"
                    UPDATE koch.SocialAccounts SET
                        SiteId=@SiteId, Provider=@Provider, AccountType=@AccountType, ExternalId=@ExternalId,
                        DisplayName=@DisplayName, Username=@Username, IsActive=@IsActive,
                        LinkedSiteBotId=@LinkedSiteBotId, Notes=@Notes, UpdatedAt=SYSUTCDATETIME()
                    WHERE Id=@Id", account);
                return account.Id;
            }

            return await conn.ExecuteScalarAsync<int>(@"
                INSERT INTO koch.SocialAccounts
                    (SiteId, Provider, AccountType, ExternalId, DisplayName, Username, IsActive, LinkedSiteBotId, Notes)
                VALUES
                    (@SiteId, @Provider, @AccountType, @ExternalId, @DisplayName, @Username, @IsActive, @LinkedSiteBotId, @Notes);
                SELECT CAST(SCOPE_IDENTITY() AS INT);", account);
        }

        public async Task SetActiveAsync(int id, bool isActive)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(
                "UPDATE koch.SocialAccounts SET IsActive=@IsActive, UpdatedAt=SYSUTCDATETIME() WHERE Id=@Id",
                new { Id = id, IsActive = isActive });
        }

        public async Task DeleteAsync(int id)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync("DELETE FROM koch.SocialAccounts WHERE Id=@Id", new { Id = id });
        }
    }

    public class SocialCredentialRepository : ISocialCredentialRepository
    {
        private readonly IDbConnectionFactory _factory;
        public SocialCredentialRepository(IDbConnectionFactory factory) => _factory = factory;

        public async Task<SocialCredential> GetByAccountIdAsync(int accountId)
        {
            try
            {
                using var conn = _factory.CreateConnection();
                return await conn.QueryFirstOrDefaultAsync<SocialCredential>(
                    "SELECT * FROM koch.SocialCredentials WHERE AccountId=@AccountId",
                    new { AccountId = accountId });
            }
            catch { return null; }
        }

        public async Task<int> UpsertAsync(SocialCredential credential)
        {
            using var conn = _factory.CreateConnection();
            var existing = await GetByAccountIdAsync(credential.AccountId);
            if (existing != null)
            {
                await conn.ExecuteAsync(@"
                    UPDATE koch.SocialCredentials SET
                        AccessTokenProtected=@AccessTokenProtected,
                        RefreshTokenProtected=@RefreshTokenProtected,
                        ApiBaseUrl=@ApiBaseUrl,
                        WebhookSecretProtected=@WebhookSecretProtected,
                        ExpiresAt=@ExpiresAt,
                        UpdatedAt=SYSUTCDATETIME()
                    WHERE AccountId=@AccountId", credential);
                return existing.Id;
            }

            return await conn.ExecuteScalarAsync<int>(@"
                INSERT INTO koch.SocialCredentials
                    (AccountId, AccessTokenProtected, RefreshTokenProtected, ApiBaseUrl, WebhookSecretProtected, ExpiresAt)
                VALUES
                    (@AccountId, @AccessTokenProtected, @RefreshTokenProtected, @ApiBaseUrl, @WebhookSecretProtected, @ExpiresAt);
                SELECT CAST(SCOPE_IDENTITY() AS INT);", credential);
        }

        public async Task DeleteByAccountIdAsync(int accountId)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync("DELETE FROM koch.SocialCredentials WHERE AccountId=@AccountId", new { AccountId = accountId });
        }
    }

    public class SocialTargetRepository : ISocialTargetRepository
    {
        private readonly IDbConnectionFactory _factory;
        public SocialTargetRepository(IDbConnectionFactory factory) => _factory = factory;

        public async Task<List<SocialTarget>> ListAsync(int? accountId = null, int? siteId = null, bool activeOnly = false)
        {
            try
            {
                using var conn = _factory.CreateConnection();
                var rows = await conn.QueryAsync<SocialTarget>(@"
                    SELECT * FROM koch.SocialTargets
                    WHERE (@AccountId IS NULL OR AccountId=@AccountId)
                      AND (@SiteId IS NULL OR SiteId=@SiteId)
                      AND (@ActiveOnly = 0 OR IsActive = 1)
                    ORDER BY Title",
                    new { AccountId = accountId, SiteId = siteId, ActiveOnly = activeOnly ? 1 : 0 });
                return rows?.ToList() ?? new List<SocialTarget>();
            }
            catch { return new List<SocialTarget>(); }
        }

        public async Task<SocialTarget> GetByIdAsync(int id)
        {
            try
            {
                using var conn = _factory.CreateConnection();
                return await conn.QueryFirstOrDefaultAsync<SocialTarget>(
                    "SELECT * FROM koch.SocialTargets WHERE Id=@Id", new { Id = id });
            }
            catch { return null; }
        }

        public async Task<int> UpsertAsync(SocialTarget target)
        {
            using var conn = _factory.CreateConnection();
            if (target.Id > 0)
            {
                await conn.ExecuteAsync(@"
                    UPDATE koch.SocialTargets SET
                        AccountId=@AccountId, SiteId=@SiteId, Provider=@Provider, Title=@Title,
                        Username=@Username, ChatId=@ChatId, ExternalTargetId=@ExternalTargetId,
                        IsActive=@IsActive, LinkedOutboundChannelId=@LinkedOutboundChannelId,
                        UpdatedAt=SYSUTCDATETIME()
                    WHERE Id=@Id", target);
                return target.Id;
            }

            return await conn.ExecuteScalarAsync<int>(@"
                INSERT INTO koch.SocialTargets
                    (AccountId, SiteId, Provider, Title, Username, ChatId, ExternalTargetId, IsActive, LinkedOutboundChannelId)
                VALUES
                    (@AccountId, @SiteId, @Provider, @Title, @Username, @ChatId, @ExternalTargetId, @IsActive, @LinkedOutboundChannelId);
                SELECT CAST(SCOPE_IDENTITY() AS INT);", target);
        }

        public async Task DeleteAsync(int id)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync("DELETE FROM koch.SocialTargets WHERE Id=@Id", new { Id = id });
        }
    }

    public class SocialConversationRepository : ISocialConversationRepository
    {
        private readonly IDbConnectionFactory _factory;
        public SocialConversationRepository(IDbConnectionFactory factory) => _factory = factory;

        public async Task<List<SocialConversation>> ListAsync(int? accountId = null, string provider = null, int top = 100)
        {
            try
            {
                using var conn = _factory.CreateConnection();
                var rows = await conn.QueryAsync<SocialConversation>(@"
                    SELECT TOP (@Top) * FROM koch.SocialConversations
                    WHERE (@AccountId IS NULL OR AccountId=@AccountId)
                      AND (@Provider IS NULL OR Provider=@Provider)
                    ORDER BY ISNULL(LastMessageAt, CreatedAt) DESC",
                    new
                    {
                        Top = top <= 0 ? 100 : top,
                        AccountId = accountId,
                        Provider = string.IsNullOrWhiteSpace(provider) ? null : provider.Trim()
                    });
                return rows?.ToList() ?? new List<SocialConversation>();
            }
            catch { return new List<SocialConversation>(); }
        }

        public async Task<SocialConversation> GetOrCreateAsync(int accountId, string provider, string externalThreadId, string participantName = null, string participantExternalId = null)
        {
            using var conn = _factory.CreateConnection();
            var existing = await conn.QueryFirstOrDefaultAsync<SocialConversation>(@"
                SELECT * FROM koch.SocialConversations
                WHERE AccountId=@AccountId AND ExternalThreadId=@ExternalThreadId",
                new { AccountId = accountId, ExternalThreadId = externalThreadId });
            if (existing != null) return existing;

            var id = await conn.ExecuteScalarAsync<int>(@"
                INSERT INTO koch.SocialConversations
                    (AccountId, Provider, ExternalThreadId, ParticipantName, ParticipantExternalId, LastMessageAt)
                VALUES
                    (@AccountId, @Provider, @ExternalThreadId, @ParticipantName, @ParticipantExternalId, SYSUTCDATETIME());
                SELECT CAST(SCOPE_IDENTITY() AS INT);",
                new
                {
                    AccountId = accountId,
                    Provider = provider,
                    ExternalThreadId = externalThreadId,
                    ParticipantName = participantName,
                    ParticipantExternalId = participantExternalId
                });
            return await conn.QueryFirstOrDefaultAsync<SocialConversation>(
                "SELECT * FROM koch.SocialConversations WHERE Id=@Id", new { Id = id });
        }

        public async Task TouchAsync(int conversationId, DateTime lastMessageAt, bool incrementUnread = true)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(@"
                UPDATE koch.SocialConversations SET
                    LastMessageAt=@LastMessageAt,
                    UnreadCount = CASE WHEN @Inc = 1 THEN UnreadCount + 1 ELSE UnreadCount END,
                    UpdatedAt=SYSUTCDATETIME()
                WHERE Id=@Id",
                new { Id = conversationId, LastMessageAt = lastMessageAt, Inc = incrementUnread ? 1 : 0 });
        }
    }

    public class SocialInboundMessageRepository : ISocialInboundMessageRepository
    {
        private readonly IDbConnectionFactory _factory;
        public SocialInboundMessageRepository(IDbConnectionFactory factory) => _factory = factory;

        public async Task<List<SocialInboundMessage>> ListByConversationAsync(int conversationId, int top = 100)
        {
            try
            {
                using var conn = _factory.CreateConnection();
                var rows = await conn.QueryAsync<SocialInboundMessage>(@"
                    SELECT TOP (@Top) * FROM koch.SocialInboundMessages
                    WHERE ConversationId=@ConversationId
                    ORDER BY ReceivedAt DESC",
                    new { ConversationId = conversationId, Top = top <= 0 ? 100 : top });
                return rows?.ToList() ?? new List<SocialInboundMessage>();
            }
            catch { return new List<SocialInboundMessage>(); }
        }

        public async Task<int> InsertIfNewAsync(SocialInboundMessage message)
        {
            using var conn = _factory.CreateConnection();
            if (!string.IsNullOrWhiteSpace(message.ExternalMessageId))
            {
                var exists = await conn.ExecuteScalarAsync<int?>(@"
                    SELECT TOP 1 Id FROM koch.SocialInboundMessages
                    WHERE Provider=@Provider AND ExternalMessageId=@ExternalMessageId",
                    new { message.Provider, message.ExternalMessageId });
                if (exists.HasValue) return exists.Value;
            }

            return await conn.ExecuteScalarAsync<int>(@"
                INSERT INTO koch.SocialInboundMessages
                    (ConversationId, Provider, ExternalMessageId, Direction, MessageType, BodyText, MediaUrl, RawPayloadJson, ReceivedAt)
                VALUES
                    (@ConversationId, @Provider, @ExternalMessageId, @Direction, @MessageType, @BodyText, @MediaUrl, @RawPayloadJson,
                     ISNULL(@ReceivedAt, SYSUTCDATETIME()));
                SELECT CAST(SCOPE_IDENTITY() AS INT);", message);
        }
    }

    public class SocialPublishJobRepository : ISocialPublishJobRepository
    {
        private readonly IDbConnectionFactory _factory;
        public SocialPublishJobRepository(IDbConnectionFactory factory) => _factory = factory;

        public async Task<List<SocialPublishJob>> ListAsync(int? siteId = null, string status = null, int top = 100)
        {
            try
            {
                using var conn = _factory.CreateConnection();
                var rows = await conn.QueryAsync<SocialPublishJob>(@"
                    SELECT TOP (@Top) * FROM koch.SocialPublishJobs
                    WHERE (@SiteId IS NULL OR SiteId=@SiteId)
                      AND (@Status IS NULL OR Status=@Status)
                    ORDER BY CreatedAt DESC",
                    new
                    {
                        Top = top <= 0 ? 100 : top,
                        SiteId = siteId,
                        Status = string.IsNullOrWhiteSpace(status) ? null : status.Trim()
                    });
                return rows?.ToList() ?? new List<SocialPublishJob>();
            }
            catch { return new List<SocialPublishJob>(); }
        }

        public async Task<List<SocialPublishJob>> ListDueAsync(DateTime utcNow, int top = 50)
        {
            try
            {
                using var conn = _factory.CreateConnection();
                var rows = await conn.QueryAsync<SocialPublishJob>(@"
                    SELECT TOP (@Top) * FROM koch.SocialPublishJobs
                    WHERE Status = @Status AND ScheduledAt <= @UtcNow
                    ORDER BY ScheduledAt ASC",
                    new { Top = top, Status = SocialPublishJobStatuses.Scheduled, UtcNow = utcNow });
                return rows?.ToList() ?? new List<SocialPublishJob>();
            }
            catch { return new List<SocialPublishJob>(); }
        }

        public async Task<SocialPublishJob> GetByIdAsync(int id)
        {
            try
            {
                using var conn = _factory.CreateConnection();
                return await conn.QueryFirstOrDefaultAsync<SocialPublishJob>(
                    "SELECT * FROM koch.SocialPublishJobs WHERE Id=@Id", new { Id = id });
            }
            catch { return null; }
        }

        public async Task<int> InsertAsync(SocialPublishJob job)
        {
            using var conn = _factory.CreateConnection();
            return await conn.ExecuteScalarAsync<int>(@"
                INSERT INTO koch.SocialPublishJobs
                    (SiteId, AccountId, TargetId, PostId, Provider, Caption, PhotoUrl, Status, ScheduledAt, AttemptCount, LastError, SentAt)
                VALUES
                    (@SiteId, @AccountId, @TargetId, @PostId, @Provider, @Caption, @PhotoUrl, @Status, @ScheduledAt, @AttemptCount, @LastError, @SentAt);
                SELECT CAST(SCOPE_IDENTITY() AS INT);", job);
        }

        public async Task UpdateStatusAsync(int id, string status, string lastError = null, DateTime? sentAt = null, int? attemptCount = null)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(@"
                UPDATE koch.SocialPublishJobs SET
                    Status=@Status,
                    LastError=@LastError,
                    SentAt=ISNULL(@SentAt, SentAt),
                    AttemptCount=ISNULL(@AttemptCount, AttemptCount),
                    UpdatedAt=SYSUTCDATETIME()
                WHERE Id=@Id",
                new { Id = id, Status = status, LastError = lastError, SentAt = sentAt, AttemptCount = attemptCount });
        }
    }

    public class SocialDeliveryStatusRepository : ISocialDeliveryStatusRepository
    {
        private readonly IDbConnectionFactory _factory;
        public SocialDeliveryStatusRepository(IDbConnectionFactory factory) => _factory = factory;

        public async Task<int> InsertAsync(SocialDeliveryStatus status)
        {
            try
            {
                using var conn = _factory.CreateConnection();
                return await conn.ExecuteScalarAsync<int>(@"
                    INSERT INTO koch.SocialDeliveryStatuses
                        (JobId, TargetId, Provider, Success, ExternalId, ErrorMessage, CheckedAt)
                    VALUES
                        (@JobId, @TargetId, @Provider, @Success, @ExternalId, @ErrorMessage, ISNULL(@CheckedAt, SYSUTCDATETIME()));
                    SELECT CAST(SCOPE_IDENTITY() AS INT);", status);
            }
            catch { return 0; }
        }

        public async Task<List<SocialDeliveryStatus>> ListByJobAsync(int jobId)
        {
            try
            {
                using var conn = _factory.CreateConnection();
                var rows = await conn.QueryAsync<SocialDeliveryStatus>(
                    "SELECT * FROM koch.SocialDeliveryStatuses WHERE JobId=@JobId ORDER BY CheckedAt DESC",
                    new { JobId = jobId });
                return rows?.ToList() ?? new List<SocialDeliveryStatus>();
            }
            catch { return new List<SocialDeliveryStatus>(); }
        }
    }

    public class SocialOperationLogRepository : ISocialOperationLogRepository
    {
        private readonly IDbConnectionFactory _factory;
        public SocialOperationLogRepository(IDbConnectionFactory factory) => _factory = factory;

        public async Task<int> InsertAsync(SocialOperationLog log)
        {
            try
            {
                using var conn = _factory.CreateConnection();
                return await conn.ExecuteScalarAsync<int>(@"
                    INSERT INTO koch.SocialOperationLogs
                        (SiteId, Provider, AccountId, Operation, Success, Detail)
                    VALUES
                        (@SiteId, @Provider, @AccountId, @Operation, @Success, @Detail);
                    SELECT CAST(SCOPE_IDENTITY() AS INT);", log);
            }
            catch { return 0; }
        }

        public async Task<List<SocialOperationLog>> ListAsync(int? siteId = null, string provider = null, int top = 200)
        {
            try
            {
                using var conn = _factory.CreateConnection();
                var rows = await conn.QueryAsync<SocialOperationLog>(@"
                    SELECT TOP (@Top) * FROM koch.SocialOperationLogs
                    WHERE (@SiteId IS NULL OR SiteId=@SiteId)
                      AND (@Provider IS NULL OR Provider=@Provider)
                    ORDER BY CreatedAt DESC",
                    new
                    {
                        Top = top <= 0 ? 200 : top,
                        SiteId = siteId,
                        Provider = string.IsNullOrWhiteSpace(provider) ? null : provider.Trim()
                    });
                return rows?.ToList() ?? new List<SocialOperationLog>();
            }
            catch { return new List<SocialOperationLog>(); }
        }
    }

    public class SocialPostRepository : ISocialPostRepository
    {
        private readonly IDbConnectionFactory _factory;
        public SocialPostRepository(IDbConnectionFactory factory) => _factory = factory;

        public async Task<int> InsertAsync(SocialPost post)
        {
            using var conn = _factory.CreateConnection();
            return await conn.ExecuteScalarAsync<int>(@"
                INSERT INTO koch.SocialPosts
                    (SiteId, AccountId, Provider, Caption, Status, ExternalPostId, PublishedAt, CreatedByUserId)
                VALUES
                    (@SiteId, @AccountId, @Provider, @Caption, @Status, @ExternalPostId, @PublishedAt, @CreatedByUserId);
                SELECT CAST(SCOPE_IDENTITY() AS INT);", post);
        }

        public async Task<SocialPost> GetByIdAsync(int id)
        {
            try
            {
                using var conn = _factory.CreateConnection();
                return await conn.QueryFirstOrDefaultAsync<SocialPost>(
                    "SELECT * FROM koch.SocialPosts WHERE Id=@Id", new { Id = id });
            }
            catch { return null; }
        }

        public async Task UpdateStatusAsync(int id, string status, string externalPostId = null, DateTime? publishedAt = null)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(@"
                UPDATE koch.SocialPosts SET
                    Status=@Status,
                    ExternalPostId=ISNULL(@ExternalPostId, ExternalPostId),
                    PublishedAt=ISNULL(@PublishedAt, PublishedAt),
                    UpdatedAt=SYSUTCDATETIME()
                WHERE Id=@Id",
                new { Id = id, Status = status, ExternalPostId = externalPostId, PublishedAt = publishedAt });
        }
    }
}
