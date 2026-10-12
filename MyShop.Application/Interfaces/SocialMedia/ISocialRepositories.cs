using MyShop.Core.Models.SocialMedia;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Interfaces.SocialMedia
{
    public interface ISocialAccountRepository
    {
        Task<List<SocialAccount>> ListAsync(int? siteId = null, string provider = null, bool activeOnly = false);
        Task<SocialAccount> GetByIdAsync(int id);
        Task<int> UpsertAsync(SocialAccount account);
        Task SetActiveAsync(int id, bool isActive);
        Task DeleteAsync(int id);
    }

    public interface ISocialCredentialRepository
    {
        Task<SocialCredential> GetByAccountIdAsync(int accountId);
        Task<int> UpsertAsync(SocialCredential credential);
        Task DeleteByAccountIdAsync(int accountId);
    }

    public interface ISocialTargetRepository
    {
        Task<List<SocialTarget>> ListAsync(int? accountId = null, int? siteId = null, bool activeOnly = false);
        Task<SocialTarget> GetByIdAsync(int id);
        Task<int> UpsertAsync(SocialTarget target);
        Task DeleteAsync(int id);
    }

    public interface ISocialConversationRepository
    {
        Task<List<SocialConversation>> ListAsync(int? accountId = null, string provider = null, int top = 100);
        Task<SocialConversation> GetOrCreateAsync(int accountId, string provider, string externalThreadId, string participantName = null, string participantExternalId = null);
        Task TouchAsync(int conversationId, DateTime lastMessageAt, bool incrementUnread = true);
    }

    public interface ISocialInboundMessageRepository
    {
        Task<List<SocialInboundMessage>> ListByConversationAsync(int conversationId, int top = 100);
        Task<int> InsertIfNewAsync(SocialInboundMessage message);
    }

    public interface ISocialPublishJobRepository
    {
        Task<List<SocialPublishJob>> ListAsync(int? siteId = null, string status = null, int top = 100);
        Task<List<SocialPublishJob>> ListDueAsync(DateTime utcNow, int top = 50);
        Task<SocialPublishJob> GetByIdAsync(int id);
        Task<int> InsertAsync(SocialPublishJob job);
        Task UpdateStatusAsync(int id, string status, string lastError = null, DateTime? sentAt = null, int? attemptCount = null);
    }

    public interface ISocialDeliveryStatusRepository
    {
        Task<int> InsertAsync(SocialDeliveryStatus status);
        Task<List<SocialDeliveryStatus>> ListByJobAsync(int jobId);
    }

    public interface ISocialOperationLogRepository
    {
        Task<int> InsertAsync(SocialOperationLog log);
        Task<List<SocialOperationLog>> ListAsync(int? siteId = null, string provider = null, int top = 200);
    }

    public interface ISocialPostRepository
    {
        Task<int> InsertAsync(SocialPost post);
        Task<SocialPost> GetByIdAsync(int id);
        Task UpdateStatusAsync(int id, string status, string externalPostId = null, DateTime? publishedAt = null);
    }
}
