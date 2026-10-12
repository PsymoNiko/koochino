using Microsoft.Extensions.Caching.Memory;
using MyShop.Application.Interfaces.Ai;
using MyShop.Core.Interfaces;
using MyShop.Core.Models.Ai;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MyShop.Application.Services.Ai
{
    /// <summary>
    /// Decides how a user message is answered. Order matters:
    /// personal-data skills, then retrieval over site knowledge, then the LLM constrained to that knowledge.
    /// A question that does not match site knowledge never reaches the LLM, which is both the scope guarantee
    /// and the main cost control.
    /// </summary>
    public class AiOrchestratorService : IAiOrchestrator
    {
        const string OutOfScopeMarker = "OUT_OF_SCOPE";
        const string IntentOutOfScope = "OutOfScope";
        static readonly Regex RefPattern = new(@"\[\[ref:\s*(\d+)\s*\]\]", RegexOptions.Compiled);
        static readonly TimeSpan CacheLifetime = TimeSpan.FromHours(6);

        private readonly List<IAiSkill> _skills;
        private readonly IKnowledgeRetrieval _retrieval;
        private readonly IAiRuntimeOptionsProvider _options;
        private readonly IAiCompletionClient _client;
        private readonly ICurrentTenant _tenant;
        private readonly IAiUsageGuard _guard;
        private readonly IAiConversationRepository _conversations;
        private readonly IAiInsightRepository _insights;
        private readonly IMemoryCache _cache;

        public AiOrchestratorService(
            IEnumerable<IAiSkill> skills,
            IKnowledgeRetrieval retrieval,
            IAiRuntimeOptionsProvider options,
            IAiCompletionClient client = null,
            ICurrentTenant tenant = null,
            IAiUsageGuard guard = null,
            IAiConversationRepository conversations = null,
            IAiInsightRepository insights = null,
            IMemoryCache cache = null)
        {
            _skills = (skills ?? Enumerable.Empty<IAiSkill>()).ToList();
            _retrieval = retrieval;
            _options = options;
            _client = client;
            _tenant = tenant;
            _guard = guard;
            _conversations = conversations;
            _insights = insights;
            _cache = cache;
        }

        public async Task<AiReply> ReplyAsync(string message, AiChatContext context = null)
        {
            if (string.IsNullOrWhiteSpace(message))
                return new AiReply("لطفاً سوال خود را بنویسید.", "Empty");

            context ??= new AiChatContext();
            if (context.SiteId == null && _tenant?.HasSite == true)
                context = context with { SiteId = _tenant.SiteId };

            var options = await ResolveOptionsAsync(context.SiteId);
            var trimmed = message.Trim();

            if (!options.Enabled)
                return await StaticFallbackAsync(trimmed, context, "Disabled");

            if (trimmed.Length > options.MaxInputChars)
            {
                return new AiReply(
                    $"پیام شما طولانی است. لطفاً سوال را در حداکثر {options.MaxInputChars} نویسه بنویسید.",
                    "InputTooLong");
            }

            if (_guard != null)
            {
                var decision = await SafeAsync(() => _guard.CheckAsync(context, trimmed, options), AiUsageDecision.Allow);
                if (!decision.Allowed)
                    return new AiReply(decision.DenyMessage, "RateLimited");
            }

            var conversationId = await EnsureConversationAsync(context, trimmed);

            // 1) Skills that answer from the signed-in user's own records. These must win over retrieval,
            //    because no knowledge article can know a specific user's point balance.
            var personalSkill = _skills
                .Where(s => s.Priority < AiSkillPriorities.StaticFaqFallback)
                .OrderBy(s => s.Priority)
                .FirstOrDefault(s => SafeCanHandle(s, trimmed));

            if (personalSkill != null)
            {
                var text = await SafeAsync(() => personalSkill.HandleAsync(trimmed, context), (string)null);
                if (!string.IsNullOrWhiteSpace(text))
                {
                    var skillReply = new AiReply(text, personalSkill.Intent);
                    await LogTurnAsync(conversationId, trimmed, skillReply, null);
                    return skillReply;
                }
            }

            // 2) Repeated questions are very common in support; serving them from cache saves tokens and latency.
            var cacheKey = BuildCacheKey(context, trimmed);
            if (_cache != null && cacheKey != null && _cache.TryGetValue(cacheKey, out AiReply cached) && cached != null)
            {
                await LogTurnAsync(conversationId, trimmed, cached, null);
                return cached;
            }

            // 3) Relevance gate over site knowledge.
            var retrieval = _retrieval == null
                ? RetrievalResult.Empty
                : await SafeAsync(() => _retrieval.RetrieveAsync(trimmed, context.SiteId, context.Audience), RetrievalResult.Empty);

            if (retrieval.BestScore < options.MinRetrievalScore)
            {
                await LogUnansweredAsync(context, trimmed, retrieval.BestScore);
                var fallback = await StaticFallbackAsync(trimmed, context, IntentOutOfScope, options);
                await LogTurnAsync(conversationId, trimmed, fallback, null);
                return fallback;
            }

            // 4) Constrained generation.
            var configured = _client != null && await SafeAsync(() => _client.IsConfiguredAsync(), false);
            if (!configured)
            {
                var fallback = await StaticFallbackAsync(trimmed, context, "Unconfigured", options, retrieval);
                await LogTurnAsync(conversationId, trimmed, fallback, null);
                return fallback;
            }

            var history = await LoadHistoryAsync(conversationId, options.HistoryTurns);
            var prompt = AiSystemPromptBuilder.Build(context.Audience, retrieval, _tenant?.SiteName);
            var completion = await SafeAsync(
                () => _client.CompleteAsync(new AiCompletionRequest(prompt, history, trimmed, 700, options.Temperature)),
                new AiCompletionResult(false, null, "خطای غیرمنتظره در تماس با سرویس هوش مصنوعی."));

            if (completion.TotalTokens > 0 && _guard != null)
                await SafeAsync(() => _guard.RecordAsync(context, completion.PromptTokens, completion.CompletionTokens));

            if (!completion.Ok)
            {
                var fallback = await StaticFallbackAsync(trimmed, context, "ProviderError", options, retrieval);
                await LogTurnAsync(conversationId, trimmed, fallback, completion);
                return fallback;
            }

            var reply = Verify(completion.Text, retrieval, options);
            if (reply == null)
            {
                await LogUnansweredAsync(context, trimmed, retrieval.BestScore);
                var rejected = OutOfScope(options, retrieval.BestScore);
                await LogTurnAsync(conversationId, trimmed, rejected, completion);
                return rejected;
            }

            if (_cache != null && cacheKey != null)
                _cache.Set(cacheKey, reply, CacheLifetime);

            await LogTurnAsync(conversationId, trimmed, reply, completion);
            return reply;
        }

        /// <summary>
        /// Grounding check. A valid reference to a retrieved article is the strong signal. When the model forgets
        /// the reference we still accept the answer if the question matched site knowledge very strongly, because
        /// scope was already proven by the gate and rejecting here would only produce a confusing dead end.
        /// </summary>
        static AiReply Verify(string raw, RetrievalResult retrieval, AiRuntimeOptions options)
        {
            var text = (raw ?? "").Trim();
            if (text.Length == 0) return null;
            if (text.IndexOf(OutOfScopeMarker, StringComparison.OrdinalIgnoreCase) >= 0) return null;

            var match = RefPattern.Match(text);
            text = RefPattern.Replace(text, "").Trim();
            if (text.Length < 4) return null;

            RetrievedArticle source = null;
            if (match.Success && int.TryParse(match.Groups[1].Value, out var refId))
                source = retrieval.Items.FirstOrDefault(x => x.Id == refId);

            if (source == null)
            {
                if (retrieval.BestScore < options.MinRetrievalScore * 2) return null;
                source = retrieval.Items.FirstOrDefault();
            }

            if (source == null) return null;

            if (!string.IsNullOrWhiteSpace(source.Slug))
                text += $"\n\nجزئیات: /help/{source.Slug}";

            return new AiReply(text, "Knowledge", UsedExternalProvider: true)
            {
                RetrievalScore = retrieval.BestScore,
                Grounded = true,
                Sources = new[] { new AiReplySource(source.Id, source.Title, source.Slug) }
            };
        }

        static AiReply OutOfScope(AiRuntimeOptions options, int score) =>
            new(options.OutOfScopeMessage, IntentOutOfScope, SuggestHandoff: true) { RetrievalScore = score };

        /// <summary>
        /// Last resort when the LLM is unavailable, disabled, or the gate rejected the question: the in-code FAQ
        /// corpus. It only matches site-specific keywords, so an off-topic question still gets the refusal.
        /// </summary>
        async Task<AiReply> StaticFallbackAsync(
            string message, AiChatContext context, string intent,
            AiRuntimeOptions options = null, RetrievalResult retrieval = null)
        {
            options ??= AiRuntimeOptions.Defaults;

            if (retrieval != null && retrieval.HasContent)
            {
                var top = retrieval.Items.First();
                var body = $"{top.Title}\n{top.Text}";
                if (!string.IsNullOrWhiteSpace(top.Slug))
                    body += $"\n\nجزئیات: /help/{top.Slug}";
                return new AiReply(body, "Knowledge", SuggestHandoff: true)
                {
                    RetrievalScore = retrieval.BestScore,
                    Grounded = true,
                    Sources = new[] { new AiReplySource(top.Id, top.Title, top.Slug) }
                };
            }

            var faqSkill = _skills
                .Where(s => s.Priority >= AiSkillPriorities.StaticFaqFallback)
                .OrderBy(s => s.Priority)
                .FirstOrDefault(s => SafeCanHandle(s, message));

            if (faqSkill != null)
            {
                var text = await SafeAsync(() => faqSkill.HandleAsync(message, context), (string)null);
                if (!string.IsNullOrWhiteSpace(text))
                    return new AiReply(text, faqSkill.Intent, SuggestHandoff: intent == IntentOutOfScope);
            }

            return OutOfScope(options, retrieval?.BestScore ?? 0);
        }

        async Task<AiRuntimeOptions> ResolveOptionsAsync(int? siteId)
        {
            if (_options == null) return AiRuntimeOptions.Defaults;
            return await SafeAsync(() => _options.GetAsync(siteId), AiRuntimeOptions.Defaults) ?? AiRuntimeOptions.Defaults;
        }

        string BuildCacheKey(AiChatContext context, string message)
        {
            var key = PersianTextScorer.CacheKey(message);
            if (string.IsNullOrWhiteSpace(key)) return null;
            return $"ai:reply:{context.SiteId ?? 0}:{context.Audience}:{key}";
        }

        async Task<Guid?> EnsureConversationAsync(AiChatContext context, string firstMessage)
        {
            if (_conversations == null) return null;
            Guid? requested = Guid.TryParse(context.ConversationId, out var parsed) ? parsed : null;
            try
            {
                return await _conversations.EnsureConversationAsync(
                    requested, context.SiteId, context.UserId, context.GuestToken,
                    context.Audience.ToString(), firstMessage);
            }
            catch
            {
                return null;
            }
        }

        async Task<List<AiChatMessage>> LoadHistoryAsync(Guid? conversationId, int turns)
        {
            if (_conversations == null || conversationId == null || turns <= 0)
                return new List<AiChatMessage>();
            try
            {
                var rows = await _conversations.GetRecentMessagesAsync(conversationId.Value, turns);
                return (rows ?? new List<AiMessageRow>())
                    .Where(r => !string.IsNullOrWhiteSpace(r.Content))
                    .Select(r => new AiChatMessage(r.Role, r.Content))
                    .ToList();
            }
            catch
            {
                return new List<AiChatMessage>();
            }
        }

        async Task LogTurnAsync(Guid? conversationId, string userMessage, AiReply reply, AiCompletionResult completion)
        {
            if (_conversations == null || conversationId == null) return;
            try
            {
                await _conversations.AddMessageAsync(new AiMessageRow
                {
                    ConversationId = conversationId.Value,
                    Role = AiMessageRoles.User,
                    Content = userMessage
                });
                await _conversations.AddMessageAsync(new AiMessageRow
                {
                    ConversationId = conversationId.Value,
                    Role = AiMessageRoles.Assistant,
                    Content = reply?.Text,
                    Intent = reply?.Intent,
                    Provider = reply?.UsedExternalProvider == true ? "OpenAICompatible" : "Local",
                    PromptTokens = completion?.PromptTokens ?? 0,
                    CompletionTokens = completion?.CompletionTokens ?? 0,
                    RetrievalScore = reply?.RetrievalScore ?? 0,
                    WasGrounded = reply?.Grounded ?? false,
                    Model = completion?.Model
                });
            }
            catch { }
        }

        async Task LogUnansweredAsync(AiChatContext context, string message, int bestScore)
        {
            if (_insights == null) return;
            try
            {
                await _insights.LogUnansweredAsync(
                    context.SiteId ?? 0,
                    context.Audience.ToString(),
                    PersianTextScorer.CacheKey(message, 400),
                    message,
                    bestScore);
            }
            catch { }
        }

        static bool SafeCanHandle(IAiSkill skill, string message)
        {
            try { return skill.CanHandle(message); }
            catch { return false; }
        }

        static async Task<T> SafeAsync<T>(Func<Task<T>> action, T fallback)
        {
            try { return await action(); }
            catch { return fallback; }
        }

        static async Task SafeAsync(Func<Task> action)
        {
            try { await action(); }
            catch { }
        }
    }
}
