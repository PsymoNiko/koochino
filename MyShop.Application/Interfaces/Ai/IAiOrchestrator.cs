using MyShop.Core.Models.Ai;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Interfaces.Ai
{
    public record AiChatContext(
        int? UserId = null,
        string ConversationId = null,
        AiAudience Audience = AiAudience.Customer,
        int? SiteId = null,
        string GuestToken = null,
        string ClientIp = null);

    public record AiReplySource(int ArticleId, string Title, string Slug);

    public record AiReply(string Text, string Intent, bool UsedExternalProvider = false, bool SuggestHandoff = false)
    {
        /// <summary>Best knowledge-base match score behind this answer (0 when no retrieval ran).</summary>
        public int RetrievalScore { get; init; }

        /// <summary>True only when the answer was produced from retrieved site knowledge.</summary>
        public bool Grounded { get; init; }

        public IReadOnlyList<AiReplySource> Sources { get; init; } = Array.Empty<AiReplySource>();
    }

    public interface IAiOrchestrator
    {
        Task<AiReply> ReplyAsync(string message, AiChatContext context = null);
    }

    public record AiChatMessage(string Role, string Content);

    public record AiCompletionRequest(
        string SystemPrompt,
        IReadOnlyList<AiChatMessage> History,
        string UserMessage,
        int MaxTokens = 700,
        double Temperature = 0.2);

    public record AiCompletionResult(
        bool Ok,
        string Text,
        string Error,
        int PromptTokens = 0,
        int CompletionTokens = 0,
        string Model = null,
        int LatencyMs = 0)
    {
        public int TotalTokens => PromptTokens + CompletionTokens;
    }

    public interface IAiCompletionClient
    {
        /// <summary>
        /// Resolves the key through the same path <see cref="CompleteAsync"/> uses, so a key stored only in
        /// encrypted module settings is detected correctly.
        /// </summary>
        Task<bool> IsConfiguredAsync();

        Task<AiCompletionResult> CompleteAsync(AiCompletionRequest request);
    }

    public record AiEmbeddingResult(
        bool Ok,
        float[] Vector,
        int Dimensions = 0,
        int Tokens = 0,
        string Model = null,
        string Error = null,
        int LatencyMs = 0);

    /// <summary>OpenAI-compatible /v1/embeddings. Failures return <see cref="AiEmbeddingResult.Ok"/> = false; they never throw.</summary>
    public interface IAiEmbeddingClient
    {
        Task<bool> IsConfiguredAsync();
        Task<string> ResolveEmbeddingModelAsync(int? siteId = null);
        Task<AiEmbeddingResult> EmbedAsync(string input, int? siteId = null);
        Task<IReadOnlyList<AiEmbeddingResult>> EmbedBatchAsync(IReadOnlyList<string> inputs, int? siteId = null);
    }

    /// <summary>Lower runs first. Skills that answer from the signed-in user's own data must stay ahead of retrieval.</summary>
    public static class AiSkillPriorities
    {
        public const int PersonalData = 10;
        public const int Advisor = 100;
        public const int StaticFaqFallback = 900;
    }

    public interface IAiSkill
    {
        string Intent { get; }
        int Priority { get; }
        bool CanHandle(string normalizedMessage);
        Task<string> HandleAsync(string message, AiChatContext context);
    }
}
