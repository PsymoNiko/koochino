using System;

namespace MyShop.Core.Models.Ai
{
    public static class AiMessageRoles
    {
        public const string User = "user";
        public const string Assistant = "assistant";
        public const string System = "system";
    }

    public static class AiUsageScopes
    {
        public const string User = "User";
        public const string Guest = "Guest";
        public const string Ip = "Ip";
        public const string Site = "Site";
    }

    public static class AiUnansweredStatuses
    {
        public const string Open = "Open";
        public const string Resolved = "Resolved";
        public const string Ignored = "Ignored";
    }

    public class AiConversation
    {
        public Guid Id { get; set; }
        public int? SiteId { get; set; }
        public int? UserId { get; set; }
        public string GuestToken { get; set; }
        public string Audience { get; set; }
        public string Title { get; set; }
        public string Status { get; set; } = "Open";
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class AiMessageRow
    {
        public long Id { get; set; }
        public Guid ConversationId { get; set; }
        public string Role { get; set; }
        public string Content { get; set; }
        public string Intent { get; set; }
        public string Provider { get; set; }
        public int PromptTokens { get; set; }
        public int CompletionTokens { get; set; }
        public int RetrievalScore { get; set; }
        public bool WasGrounded { get; set; }
        public string Model { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class AiUsageSnapshot
    {
        public int UserMessages { get; set; }
        public int GuestMessages { get; set; }
        public int IpMessages { get; set; }
        public int SiteTokens { get; set; }

        /// <summary>Highest per-identity message count for today, whichever identity applies.</summary>
        public int IdentityMessages => Math.Max(UserMessages, Math.Max(GuestMessages, IpMessages));
    }

    public class AiUsageDailyRow
    {
        public DateTime UsageDate { get; set; }
        public int Messages { get; set; }
        public int PromptTokens { get; set; }
        public int CompletionTokens { get; set; }
        public int TotalTokens => PromptTokens + CompletionTokens;
    }

    public class AiUnansweredQuestion
    {
        public long Id { get; set; }
        public int SiteId { get; set; }
        public string Audience { get; set; }
        public string NormalizedQuestion { get; set; }
        public string SampleQuestion { get; set; }
        public int HitCount { get; set; }
        public int BestScore { get; set; }
        public string Status { get; set; }
        public int? CreatedArticleId { get; set; }
        public DateTime FirstSeenAt { get; set; }
        public DateTime LastSeenAt { get; set; }
    }

    public static class AiLearnDefaults
    {
        public const string SourcePrefix = "chat:";
        public const int MinQuestionChars = 12;
        public const int MinAnswerChars = 40;
        public const byte MinCsatScore = 4;
        public const int MaxPairsPerChat = 2;
        public const int ChatsPerRun = 8;

        public static string SourceKey(Guid chatId, long userMessageId)
            => SourcePrefix + chatId.ToString("N") + ":" + userMessageId;
    }

    public class AiKbLearnCursor
    {
        public int SiteId { get; set; }
        public DateTime? LastClosedAt { get; set; }
        public DateTime LastRunAt { get; set; }
        public string LastResult { get; set; }
    }

    public record AdminChatPair(long UserMessageId, string Question, long AdminMessageId, string Answer);

    public record AiLearnRunResult(int ChatsScanned, int DraftsCreated, int Skipped, int Tokens, string Error = null)
    {
        public bool Ok => string.IsNullOrEmpty(Error);
    }

    public static class AiEmbeddingDefaults
    {
        public const string Model = "text-embedding-3-small";
        public const int Dimensions = 512;
        public const int SemanticTopK = 8;
        public const float SemanticFloor = 0.55f;
    }

    public class AiKnowledgeChunk
    {
        public long Id { get; set; }
        public int ArticleId { get; set; }
        public int? SiteId { get; set; }
        public int ChunkIndex { get; set; }
        public string Text { get; set; }
        public string ContentHash { get; set; }
        public string SourceHash { get; set; }
        public byte[] Embedding { get; set; }
        public int Dimensions { get; set; }
        public string Model { get; set; }
        public DateTime UpdatedAt { get; set; }

        public string Title { get; set; }
        public string Slug { get; set; }
        public string Audience { get; set; }
        public string ArticleType { get; set; }
    }
}
