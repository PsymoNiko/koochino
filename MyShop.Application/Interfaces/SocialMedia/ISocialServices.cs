using MyShop.Core.Models.SocialMedia;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MyShop.Application.Interfaces.SocialMedia
{
    public interface ISocialAccountService
    {
        /// <summary>لیست حساب‌های Social* به‌همراه پل اختیاری از SiteBots (فقط خواندنی).</summary>
        Task<List<SocialAccount>> ListConnectedAsync(int? siteId = null, string provider = null, bool includeSiteBotBridge = true);
        Task<SocialAccount> GetAsync(int id);
        Task<int> SaveAsync(SocialAccount account);
        Task SetActiveAsync(int id, bool isActive);
        Task DeleteAsync(int id);
        Task<IReadOnlyList<ISocialProvider>> ListProvidersAsync();
    }

    public interface ISocialInboxService
    {
        Task<List<SocialConversation>> ListConversationsAsync(int? accountId = null, string provider = null, int top = 100);
        Task<List<SocialInboundMessage>> ListMessagesAsync(int conversationId, int top = 100);
        Task<int> SyncAccountAsync(int accountId, CancellationToken ct = default);
    }

    public interface ISocialPublishService
    {
        Task<SocialResult> PublishNowAsync(SocialPublishRequest request, CancellationToken ct = default);
        Task<int> ScheduleAsync(SocialPublishRequest request);
        Task<List<SocialPublishJob>> ListJobsAsync(int? siteId = null, string status = null, int top = 100);
    }

    public interface ISocialScheduleService
    {
        Task<int> ProcessDueJobsAsync(CancellationToken ct = default);
    }

    public interface ISocialWebhookIntakeService
    {
        Task<SocialWebhookIntakeResult> IngestAsync(SocialWebhookContext context, CancellationToken ct = default);
    }

    public interface ISocialOperationLogService
    {
        Task LogAsync(int? siteId, string provider, int? accountId, string operation, bool success, string detail = null);
        Task<List<SocialOperationLog>> ListAsync(int? siteId = null, string provider = null, int top = 200);
    }
}
