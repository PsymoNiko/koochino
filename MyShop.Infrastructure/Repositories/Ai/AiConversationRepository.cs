using Dapper;
using MyShop.Application.Interfaces.Ai;
using MyShop.Core.Interfaces;
using MyShop.Core.Models.Ai;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Infrastructure.Repositories.Ai
{
    /// <summary>
    /// Persists assistant conversations on koch.AiConversations / koch.AiMessages so the bot has memory
    /// across turns and every token spent is auditable.
    /// </summary>
    public class AiConversationRepository : IAiConversationRepository
    {
        private readonly IDbConnectionFactory _factory;
        public AiConversationRepository(IDbConnectionFactory factory) => _factory = factory;

        public async Task<Guid> EnsureConversationAsync(
            Guid? conversationId, int? siteId, int? userId, string guestToken, string audience, string firstMessage)
        {
            using var conn = _factory.CreateConnection();
            var id = conversationId ?? Guid.NewGuid();

            // UserId has a FK to koch.Users, so guests must stay NULL rather than 0.
            var normalizedUserId = userId is > 0 ? userId : null;

            await conn.ExecuteAsync(@"
IF NOT EXISTS (SELECT 1 FROM koch.AiConversations WHERE Id=@Id)
    INSERT INTO koch.AiConversations (Id, SiteId, UserId, GuestToken, Audience, Title, Status, CreatedAt, UpdatedAt)
    VALUES (@Id, @SiteId, @UserId, @GuestToken, @Audience, @Title, N'Open', SYSDATETIME(), SYSDATETIME())
ELSE
    UPDATE koch.AiConversations SET UpdatedAt=SYSDATETIME() WHERE Id=@Id;",
                new
                {
                    Id = id,
                    SiteId = siteId is > 0 ? siteId : null,
                    UserId = normalizedUserId,
                    GuestToken = Trunc(guestToken, 64),
                    Audience = Trunc(audience, 24),
                    Title = Trunc(firstMessage, 200)
                });

            return id;
        }

        public async Task AddMessageAsync(AiMessageRow message)
        {
            if (message == null || message.ConversationId == Guid.Empty) return;
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(@"
INSERT INTO koch.AiMessages
 (ConversationId, Role, Content, Intent, Provider, PromptTokens, CompletionTokens, RetrievalScore, WasGrounded, Model, CreatedAt)
VALUES
 (@ConversationId, @Role, @Content, @Intent, @Provider, @PromptTokens, @CompletionTokens, @RetrievalScore, @WasGrounded, @Model, SYSDATETIME());
UPDATE koch.AiConversations SET UpdatedAt=SYSDATETIME() WHERE Id=@ConversationId;",
                new
                {
                    message.ConversationId,
                    Role = Trunc(message.Role, 24),
                    Content = message.Content ?? "",
                    Intent = Trunc(message.Intent, 64),
                    Provider = Trunc(message.Provider, 64),
                    message.PromptTokens,
                    message.CompletionTokens,
                    message.RetrievalScore,
                    message.WasGrounded,
                    Model = Trunc(message.Model, 80)
                });
        }

        public async Task<List<AiMessageRow>> GetRecentMessagesAsync(Guid conversationId, int take)
        {
            if (conversationId == Guid.Empty || take <= 0) return new List<AiMessageRow>();
            using var conn = _factory.CreateConnection();
            var rows = await conn.QueryAsync<AiMessageRow>(@"
SELECT TOP (@Take) *
FROM koch.AiMessages
WHERE ConversationId=@ConversationId AND Role IN (N'user', N'assistant')
ORDER BY Id DESC", new { ConversationId = conversationId, Take = take });

            // Reversed so the oldest of the window is sent to the model first.
            return rows?.Reverse().ToList() ?? new List<AiMessageRow>();
        }

        static string Trunc(string s, int n)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            s = s.Trim();
            return s.Length <= n ? s : s.Substring(0, n);
        }
    }
}
