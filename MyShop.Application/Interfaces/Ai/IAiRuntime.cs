using MyShop.Core.Models.Ai;
using MyShop.Core.Models.SupportChat;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Interfaces.Ai
{
    public record RetrievedArticle(int Id, string Title, string Slug, string Text, int Score);

    public record RetrievalResult(IReadOnlyList<RetrievedArticle> Items, int BestScore)
    {
        public static readonly RetrievalResult Empty = new(Array.Empty<RetrievedArticle>(), 0);
        public bool HasContent => Items != null && Items.Count > 0;
    }

    public interface IKnowledgeRetrieval
    {
        Task<RetrievalResult> RetrieveAsync(string query, int? siteId, AiAudience audience, int topK = 4);

        /// <summary>Same keyword path as phase 1, ignoring embeddings. Used by diagnostics to compare recall.</summary>
        Task<RetrievalResult> RetrieveKeywordOnlyAsync(string query, int? siteId, AiAudience audience, int topK = 4)
            => RetrieveAsync(query, siteId, audience, topK);
    }

    /// <summary>Per-site AI behaviour resolved from support settings.</summary>
    public record AiRuntimeOptions(
        bool Enabled,
        int MinRetrievalScore,
        int MaxMessagesPerIdentityPerDay,
        int MaxTokensPerSitePerDay,
        int MaxInputChars,
        int HistoryTurns,
        double Temperature,
        string OutOfScopeMessage,
        bool EmbeddingsEnabled = false,
        bool LearnFromAdminChat = false)
    {
        public static readonly AiRuntimeOptions Defaults = new(
            Enabled: true,
            MinRetrievalScore: 24,
            MaxMessagesPerIdentityPerDay: 40,
            MaxTokensPerSitePerDay: 300000,
            MaxInputChars: 600,
            HistoryTurns: 8,
            Temperature: 0.2,
            OutOfScopeMessage: "این سوال خارج از حوزه پشتیبانی این سایت است و پاسخی برای آن ندارم. اگر سوال شما درباره خدمات ما است، لطفاً واضح‌تر بپرسید یا با پشتیبانی انسانی گفتگو کنید.",
            EmbeddingsEnabled: false,
            LearnFromAdminChat: false);
    }

    public interface IAiRuntimeOptionsProvider
    {
        Task<AiRuntimeOptions> GetAsync(int? siteId);
    }

    public record AiUsageDecision(bool Allowed, string DenyMessage)
    {
        public static readonly AiUsageDecision Allow = new(true, null);
        public static AiUsageDecision Deny(string message) => new(false, message);
    }

    public interface IAiUsageGuard
    {
        Task<AiUsageDecision> CheckAsync(AiChatContext context, string message, AiRuntimeOptions options);
        Task RecordAsync(AiChatContext context, int promptTokens, int completionTokens);
    }

    public interface IAiConversationRepository
    {
        Task<Guid> EnsureConversationAsync(Guid? conversationId, int? siteId, int? userId, string guestToken, string audience, string firstMessage);
        Task AddMessageAsync(AiMessageRow message);
        Task<List<AiMessageRow>> GetRecentMessagesAsync(Guid conversationId, int take);
    }

    public interface IAiUsageRepository
    {
        Task<AiUsageSnapshot> GetSnapshotAsync(int siteId, DateTime day, string userKey, string guestKey, string ipKey);
        Task IncrementAsync(int siteId, DateTime day, string scope, string scopeKey, int messages, int promptTokens, int completionTokens);
        Task<List<AiUsageDailyRow>> GetDailyAsync(int siteId, int days);
    }

    public record KbSeedResult(int Created, int Updated, int Skipped, string Error = null)
    {
        public bool Ok => string.IsNullOrEmpty(Error);
        public int Total => Created + Updated;
    }

    /// <summary>Copies the in-code FAQ corpus into the database knowledge base so retrieval has content.</summary>
    public interface IKbSeedImporter
    {
        Task<KbSeedResult> ImportAsync(int? siteId, int? authorUserId = null);
    }

    public interface IAiInsightRepository
    {
        Task LogUnansweredAsync(int siteId, string audience, string normalizedQuestion, string sampleQuestion, int bestScore);
        Task<List<AiUnansweredQuestion>> ListUnansweredAsync(int siteId, string status, int take);
        Task SetUnansweredStatusAsync(long id, string status, int? createdArticleId);
        Task<int> CountGroundedAsync(int siteId, int days);
        Task<int> CountRejectedAsync(int siteId, int days);
    }

    public record AiIndexResult(int ArticlesProcessed, int ChunksWritten, int Skipped, int Tokens, string Error = null)
    {
        public bool Ok => string.IsNullOrEmpty(Error);
    }

    public interface IAiKnowledgeChunkRepository
    {
        Task ReplaceArticleChunksAsync(int articleId, IReadOnlyList<AiKnowledgeChunk> chunks);
        Task DeleteByArticleAsync(int articleId);
        Task<List<AiKnowledgeChunk>> GetByArticleAsync(int articleId);
        Task<List<AiKnowledgeChunk>> ListPublishedAsync(int? siteId, string audience, string model);
        Task<List<int>> ListStaleArticleIdsAsync(int? siteId, int take);
        Task<int> CountAsync(int? siteId);
    }

    public interface IAiEmbeddingIndexer
    {
        Task<AiIndexResult> IndexArticleAsync(int articleId);
        Task<AiIndexResult> IndexStaleAsync(int? siteId, int take = 8);
        Task<AiIndexResult> RebuildSiteAsync(int? siteId);
    }

    public interface IAiKbLearnRepository
    {
        Task<AiKbLearnCursor> GetCursorAsync(int siteId);
        Task SaveCursorAsync(int siteId, DateTime? lastClosedAt, string result);
        Task<List<int>> ListEnabledSiteIdsAsync();
        Task<List<SupportChatThread>> ListClosedAfterAsync(int? siteId, DateTime? afterClosedAt, int take);
    }

    public interface IAiKbDraftLearner
    {
        Task<AiLearnRunResult> ProcessSiteAsync(int? siteId);
    }
}
