using MyShop.Application.Interfaces.SocialMedia;
using MyShop.Core.Models.SocialMedia;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MyShop.Application.Services.SocialMedia
{
    public class SocialInboxService : ISocialInboxService
    {
        private readonly ISocialConversationRepository _conversations;
        private readonly ISocialInboundMessageRepository _messages;
        private readonly ISocialAccountRepository _accounts;
        private readonly ISocialProviderResolver _providers;
        private readonly ISocialOperationLogService _logs;

        public SocialInboxService(
            ISocialConversationRepository conversations,
            ISocialInboundMessageRepository messages,
            ISocialAccountRepository accounts,
            ISocialProviderResolver providers,
            ISocialOperationLogService logs)
        {
            _conversations = conversations;
            _messages = messages;
            _accounts = accounts;
            _providers = providers;
            _logs = logs;
        }

        public Task<List<SocialConversation>> ListConversationsAsync(int? accountId = null, string provider = null, int top = 100) =>
            _conversations.ListAsync(accountId, provider, top);

        public Task<List<SocialInboundMessage>> ListMessagesAsync(int conversationId, int top = 100) =>
            _messages.ListByConversationAsync(conversationId, top);

        public async Task<int> SyncAccountAsync(int accountId, CancellationToken ct = default)
        {
            var account = await _accounts.GetByIdAsync(accountId);
            if (account == null)
                return 0;

            var reader = _providers.GetInboxReader(account.Provider);
            if (reader == null)
            {
                await _logs.LogAsync(account.SiteId, account.Provider, accountId, "InboxSync", false, "Provider inbox not available");
                return 0;
            }

            var count = await reader.SyncInboxAsync(accountId, ct);
            await _logs.LogAsync(account.SiteId, account.Provider, accountId, "InboxSync", true, $"synced={count}");
            return count;
        }
    }

    public class SocialPublishService : ISocialPublishService
    {
        private readonly ISocialPublishJobRepository _jobs;
        private readonly ISocialDeliveryStatusRepository _delivery;
        private readonly ISocialProviderResolver _providers;
        private readonly ISocialOperationLogService _logs;

        public SocialPublishService(
            ISocialPublishJobRepository jobs,
            ISocialDeliveryStatusRepository delivery,
            ISocialProviderResolver providers,
            ISocialOperationLogService logs)
        {
            _jobs = jobs;
            _delivery = delivery;
            _providers = providers;
            _logs = logs;
        }

        public async Task<SocialResult> PublishNowAsync(SocialPublishRequest request, CancellationToken ct = default)
        {
            if (request == null) return SocialResult.Fail("درخواست خالی است.");
            var publisher = _providers.GetPublisher(request.Provider);
            if (publisher == null)
                return SocialResult.Fail($"Provider «{request.Provider}» ثبت نشده است.");

            var result = await publisher.PublishAsync(request, ct);
            await _logs.LogAsync(request.SiteId, request.Provider, request.AccountId, "PublishNow", result.Success, result.Message);

            var job = new SocialPublishJob
            {
                SiteId = request.SiteId,
                AccountId = request.AccountId,
                TargetId = request.TargetId,
                Provider = request.Provider,
                Caption = request.Caption,
                PhotoUrl = request.PhotoUrl,
                Status = result.Success ? SocialPublishJobStatuses.Sent : SocialPublishJobStatuses.Failed,
                ScheduledAt = DateTime.UtcNow,
                AttemptCount = 1,
                LastError = result.Success ? null : result.Message,
                SentAt = result.Success ? DateTime.UtcNow : null,
                CreatedAt = DateTime.UtcNow
            };
            var jobId = await _jobs.InsertAsync(job);
            await _delivery.InsertAsync(new SocialDeliveryStatus
            {
                JobId = jobId,
                TargetId = request.TargetId,
                Provider = request.Provider,
                Success = result.Success,
                ExternalId = result.ExternalId,
                ErrorMessage = result.Success ? null : result.Message,
                CheckedAt = DateTime.UtcNow
            });

            return result;
        }

        public async Task<int> ScheduleAsync(SocialPublishRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            var when = request.ScheduleAtUtc ?? DateTime.UtcNow.AddMinutes(5);
            var job = new SocialPublishJob
            {
                SiteId = request.SiteId,
                AccountId = request.AccountId,
                TargetId = request.TargetId,
                Provider = request.Provider,
                Caption = request.Caption,
                PhotoUrl = request.PhotoUrl,
                Status = SocialPublishJobStatuses.Scheduled,
                ScheduledAt = when,
                AttemptCount = 0,
                CreatedAt = DateTime.UtcNow
            };
            var id = await _jobs.InsertAsync(job);
            await _logs.LogAsync(request.SiteId, request.Provider, request.AccountId, "PublishSchedule", true, $"job={id}; at={when:o}");
            return id;
        }

        public Task<List<SocialPublishJob>> ListJobsAsync(int? siteId = null, string status = null, int top = 100) =>
            _jobs.ListAsync(siteId, status, top);
    }

    public class SocialScheduleService : ISocialScheduleService
    {
        private readonly ISocialPublishJobRepository _jobs;
        private readonly ISocialDeliveryStatusRepository _delivery;
        private readonly ISocialProviderResolver _providers;
        private readonly ISocialOperationLogService _logs;

        public SocialScheduleService(
            ISocialPublishJobRepository jobs,
            ISocialDeliveryStatusRepository delivery,
            ISocialProviderResolver providers,
            ISocialOperationLogService logs)
        {
            _jobs = jobs;
            _delivery = delivery;
            _providers = providers;
            _logs = logs;
        }

        public async Task<int> ProcessDueJobsAsync(CancellationToken ct = default)
        {
            var due = await _jobs.ListDueAsync(DateTime.UtcNow, top: 50);
            var processed = 0;
            foreach (var job in due)
            {
                ct.ThrowIfCancellationRequested();
                await _jobs.UpdateStatusAsync(job.Id, SocialPublishJobStatuses.Queued, attemptCount: job.AttemptCount + 1);

                var publisher = _providers.GetPublisher(job.Provider);
                SocialResult result;
                if (publisher == null)
                {
                    result = SocialResult.Fail($"Provider «{job.Provider}» ثبت نشده است.");
                }
                else
                {
                    result = await publisher.PublishAsync(new SocialPublishRequest
                    {
                        SiteId = job.SiteId,
                        AccountId = job.AccountId,
                        TargetId = job.TargetId,
                        Provider = job.Provider,
                        Caption = job.Caption,
                        PhotoUrl = job.PhotoUrl
                    }, ct);
                }

                await _jobs.UpdateStatusAsync(
                    job.Id,
                    result.Success ? SocialPublishJobStatuses.Sent : SocialPublishJobStatuses.Failed,
                    result.Success ? null : result.Message,
                    result.Success ? DateTime.UtcNow : null,
                    job.AttemptCount + 1);

                await _delivery.InsertAsync(new SocialDeliveryStatus
                {
                    JobId = job.Id,
                    TargetId = job.TargetId,
                    Provider = job.Provider,
                    Success = result.Success,
                    ExternalId = result.ExternalId,
                    ErrorMessage = result.Success ? null : result.Message,
                    CheckedAt = DateTime.UtcNow
                });

                await _logs.LogAsync(job.SiteId, job.Provider, job.AccountId, "ScheduleProcess", result.Success, $"job={job.Id}; {result.Message}");
                processed++;
            }

            return processed;
        }
    }

    public class SocialWebhookIntakeService : ISocialWebhookIntakeService
    {
        private readonly ISocialProviderResolver _providers;
        private readonly ISocialOperationLogService _logs;
        private readonly ISocialConversationRepository _conversations;
        private readonly ISocialInboundMessageRepository _messages;

        public SocialWebhookIntakeService(
            ISocialProviderResolver providers,
            ISocialOperationLogService logs,
            ISocialConversationRepository conversations,
            ISocialInboundMessageRepository messages)
        {
            _providers = providers;
            _logs = logs;
            _conversations = conversations;
            _messages = messages;
        }

        public async Task<SocialWebhookIntakeResult> IngestAsync(SocialWebhookContext context, CancellationToken ct = default)
        {
            if (context == null)
                return new SocialWebhookIntakeResult { Accepted = false, Message = "context null" };

            var handler = _providers.GetWebhookHandler(context.Provider);
            SocialWebhookIntakeResult result;
            if (handler != null)
            {
                result = await handler.HandleAsync(context, ct);
            }
            else
            {
                result = new SocialWebhookIntakeResult
                {
                    Accepted = true,
                    ForwardToLegacyBotProcessor = IsBotApiProvider(context.Provider),
                    Message = "No dedicated handler; forward if legacy bot API."
                };
            }

            // Best-effort inbox capture for bot-style JSON (does not replace BaleService)
            if (!string.IsNullOrWhiteSpace(context.Body) && IsBotApiProvider(context.Provider))
            {
                try { await TryCaptureBotUpdateAsync(context); }
                catch { /* never break webhook */ }
            }

            await _logs.LogAsync(context.SiteId, context.Provider, null, "WebhookIngest", result.Accepted,
                result.Message);

            // Always forward Telegram/Bale so switching webhook URL to /api/social does not break OTP/bot menu
            if (IsBotApiProvider(context.Provider))
                result.ForwardToLegacyBotProcessor = true;

            return result;
        }

        static bool IsBotApiProvider(string provider) =>
            string.Equals(provider, SocialProviderKeys.Telegram, StringComparison.OrdinalIgnoreCase)
            || string.Equals(provider, SocialProviderKeys.Bale, StringComparison.OrdinalIgnoreCase);

        async Task TryCaptureBotUpdateAsync(SocialWebhookContext context)
        {
            // Minimal parse without depending on Telegram SDK — store raw under a synthetic thread when possible
            using var doc = System.Text.Json.JsonDocument.Parse(context.Body);
            var root = doc.RootElement;
            if (!root.TryGetProperty("message", out var message) && !root.TryGetProperty("edited_message", out message))
                return;

            var chatId = message.TryGetProperty("chat", out var chat) && chat.TryGetProperty("id", out var idEl)
                ? idEl.ToString()
                : null;
            if (string.IsNullOrWhiteSpace(chatId))
                return;

            var text = message.TryGetProperty("text", out var textEl) ? textEl.GetString() : null;
            var msgId = message.TryGetProperty("message_id", out var mid) ? mid.ToString() : null;
            var fromName = message.TryGetProperty("from", out var from) && from.TryGetProperty("first_name", out var fn)
                ? fn.GetString()
                : null;
            var fromId = message.TryGetProperty("from", out var from2) && from2.TryGetProperty("id", out var fid)
                ? fid.ToString()
                : null;

            // AccountId unknown at intake without SocialAccounts mapping — skip DB write if no account bridge
            // Store under accountId=0 is invalid FK; only log operation already done.
            _ = chatId;
            _ = text;
            _ = msgId;
            _ = fromName;
            _ = fromId;
            _ = _conversations;
            _ = _messages;
        }
    }

    public class SocialOperationLogService : ISocialOperationLogService
    {
        private readonly ISocialOperationLogRepository _repo;

        public SocialOperationLogService(ISocialOperationLogRepository repo)
        {
            _repo = repo;
        }

        public Task LogAsync(int? siteId, string provider, int? accountId, string operation, bool success, string detail = null) =>
            _repo.InsertAsync(new SocialOperationLog
            {
                SiteId = siteId,
                Provider = provider,
                AccountId = accountId,
                Operation = operation ?? "",
                Success = success,
                Detail = detail != null && detail.Length > 2000 ? detail.Substring(0, 2000) : detail,
                CreatedAt = DateTime.UtcNow
            });

        public Task<List<SocialOperationLog>> ListAsync(int? siteId = null, string provider = null, int top = 200) =>
            _repo.ListAsync(siteId, provider, top);
    }

    public class SocialProviderResolver : ISocialProviderResolver
    {
        private readonly Dictionary<string, ISocialProvider> _providers;
        private readonly Dictionary<string, ISocialPublisher> _publishers;
        private readonly Dictionary<string, ISocialMessageSender> _senders;
        private readonly Dictionary<string, ISocialWebhookHandler> _handlers;
        private readonly Dictionary<string, ISocialInboxReader> _inbox;
        private readonly Dictionary<string, ISocialCommentService> _comments;

        public SocialProviderResolver(
            IEnumerable<ISocialProvider> providers,
            IEnumerable<ISocialPublisher> publishers,
            IEnumerable<ISocialMessageSender> senders,
            IEnumerable<ISocialWebhookHandler> handlers,
            IEnumerable<ISocialInboxReader> inboxReaders,
            IEnumerable<ISocialCommentService> commentServices)
        {
            _providers = Index(providers, p => p.ProviderKey);
            _publishers = Index(publishers, p => p.ProviderKey);
            _senders = Index(senders, p => p.ProviderKey);
            _handlers = Index(handlers, p => p.ProviderKey);
            _inbox = Index(inboxReaders, p => p.ProviderKey);
            _comments = Index(commentServices, p => p.ProviderKey);
        }

        static Dictionary<string, T> Index<T>(IEnumerable<T> items, Func<T, string> keySelector)
        {
            var map = new Dictionary<string, T>(StringComparer.OrdinalIgnoreCase);
            if (items == null) return map;
            foreach (var item in items)
            {
                var key = keySelector(item);
                if (string.IsNullOrWhiteSpace(key)) continue;
                map[key.Trim()] = item;
            }
            return map;
        }

        public ISocialProvider GetProvider(string providerKey) =>
            string.IsNullOrWhiteSpace(providerKey) ? null : _providers.GetValueOrDefault(providerKey.Trim());

        public ISocialPublisher GetPublisher(string providerKey) =>
            string.IsNullOrWhiteSpace(providerKey) ? null : _publishers.GetValueOrDefault(providerKey.Trim());

        public ISocialMessageSender GetMessageSender(string providerKey) =>
            string.IsNullOrWhiteSpace(providerKey) ? null : _senders.GetValueOrDefault(providerKey.Trim());

        public ISocialWebhookHandler GetWebhookHandler(string providerKey) =>
            string.IsNullOrWhiteSpace(providerKey) ? null : _handlers.GetValueOrDefault(providerKey.Trim());

        public ISocialInboxReader GetInboxReader(string providerKey) =>
            string.IsNullOrWhiteSpace(providerKey) ? null : _inbox.GetValueOrDefault(providerKey.Trim());

        public ISocialCommentService GetCommentService(string providerKey) =>
            string.IsNullOrWhiteSpace(providerKey) ? null : _comments.GetValueOrDefault(providerKey.Trim());

        public IReadOnlyList<ISocialProvider> ListProviders() => _providers.Values.ToList();
    }
}
