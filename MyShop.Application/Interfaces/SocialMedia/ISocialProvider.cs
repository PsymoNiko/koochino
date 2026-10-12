using MyShop.Core.Models.SocialMedia;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MyShop.Application.Interfaces.SocialMedia
{
    public interface ISocialProvider
    {
        string ProviderKey { get; }
        SocialCapabilities SupportedCapabilities { get; }
        bool Supports(SocialCapabilities capability) => (SupportedCapabilities & capability) == capability;
    }

    public interface ISocialMessageSender
    {
        string ProviderKey { get; }
        Task<SocialResult> SendTextAsync(string chatOrThreadId, string text, CancellationToken ct = default);
        Task<SocialResult> SendMediaAsync(string chatOrThreadId, string mediaUrl, string caption = null, CancellationToken ct = default);
    }

    public interface ISocialPublisher
    {
        string ProviderKey { get; }
        Task<SocialResult> PublishAsync(SocialPublishRequest request, CancellationToken ct = default);
    }

    public interface ISocialInboxReader
    {
        string ProviderKey { get; }
        Task<int> SyncInboxAsync(int accountId, CancellationToken ct = default);
    }

    public interface ISocialCommentService
    {
        string ProviderKey { get; }
        Task<List<SocialComment>> ListCommentsAsync(int postId, CancellationToken ct = default);
        Task<SocialResult> ReplyAsync(int postId, string externalCommentId, string text, CancellationToken ct = default);
    }

    public interface ISocialWebhookHandler
    {
        string ProviderKey { get; }
        Task<SocialWebhookIntakeResult> HandleAsync(SocialWebhookContext context, CancellationToken ct = default);
    }

    public class SocialWebhookContext
    {
        public string Provider { get; set; } = "";
        public string SiteCode { get; set; } = "";
        public int SiteId { get; set; }
        public string Body { get; set; } = "";
        public string QueryHubMode { get; set; }
        public string QueryHubChallenge { get; set; }
        public string QueryHubVerifyToken { get; set; }
        public IDictionary<string, string> Headers { get; set; }
    }

    public interface ISocialProviderResolver
    {
        ISocialProvider GetProvider(string providerKey);
        ISocialPublisher GetPublisher(string providerKey);
        ISocialMessageSender GetMessageSender(string providerKey);
        ISocialWebhookHandler GetWebhookHandler(string providerKey);
        ISocialInboxReader GetInboxReader(string providerKey);
        ISocialCommentService GetCommentService(string providerKey);
        IReadOnlyList<ISocialProvider> ListProviders();
    }

    public interface ISocialCredentialProtector
    {
        string Protect(string plain);
        string Unprotect(string protectedValue);
    }
}
