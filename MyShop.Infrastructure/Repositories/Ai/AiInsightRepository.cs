using Dapper;
using MyShop.Application.Interfaces.Ai;
using MyShop.Core.Interfaces;
using MyShop.Core.Models.Ai;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Infrastructure.Repositories.Ai
{
    /// <summary>
    /// Quality signals for the assistant: which questions it could not answer, and how often answers were
    /// actually grounded in site knowledge.
    /// </summary>
    public class AiInsightRepository : IAiInsightRepository
    {
        private readonly IDbConnectionFactory _factory;
        public AiInsightRepository(IDbConnectionFactory factory) => _factory = factory;

        public async Task LogUnansweredAsync(int siteId, string audience, string normalizedQuestion, string sampleQuestion, int bestScore)
        {
            if (string.IsNullOrWhiteSpace(normalizedQuestion)) return;
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(@"
MERGE koch.AiUnansweredQuestions WITH (HOLDLOCK) AS t
USING (SELECT @SiteId AS SiteId, @Normalized AS NormalizedQuestion) AS s
    ON t.SiteId=s.SiteId AND t.NormalizedQuestion=s.NormalizedQuestion
WHEN MATCHED THEN UPDATE SET
    HitCount = t.HitCount + 1,
    BestScore = CASE WHEN @BestScore > t.BestScore THEN @BestScore ELSE t.BestScore END,
    SampleQuestion = ISNULL(@Sample, t.SampleQuestion),
    LastSeenAt = SYSDATETIME()
WHEN NOT MATCHED THEN INSERT (SiteId, Audience, NormalizedQuestion, SampleQuestion, HitCount, BestScore, Status, FirstSeenAt, LastSeenAt)
    VALUES (@SiteId, @Audience, @Normalized, @Sample, 1, @BestScore, N'Open', SYSDATETIME(), SYSDATETIME());",
                new
                {
                    SiteId = siteId,
                    Audience = Trunc(audience, 24),
                    Normalized = Trunc(normalizedQuestion, 400),
                    Sample = Trunc(sampleQuestion, 1000),
                    BestScore = bestScore
                });
        }

        public async Task<List<AiUnansweredQuestion>> ListUnansweredAsync(int siteId, string status, int take)
        {
            using var conn = _factory.CreateConnection();
            var rows = await conn.QueryAsync<AiUnansweredQuestion>(@"
SELECT TOP (@Take) *
FROM koch.AiUnansweredQuestions
WHERE SiteId=@SiteId
  AND (@Status IS NULL OR @Status=N'' OR Status=@Status)
ORDER BY HitCount DESC, LastSeenAt DESC",
                new { SiteId = siteId, Status = status, Take = take <= 0 ? 100 : take });
            return rows?.ToList() ?? new List<AiUnansweredQuestion>();
        }

        public async Task SetUnansweredStatusAsync(long id, string status, int? createdArticleId)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(@"
UPDATE koch.AiUnansweredQuestions
SET Status=@Status,
    CreatedArticleId=ISNULL(@ArticleId, CreatedArticleId)
WHERE Id=@Id", new { Id = id, Status = status, ArticleId = createdArticleId });
        }

        public async Task<int> CountGroundedAsync(int siteId, int days)
        {
            using var conn = _factory.CreateConnection();
            return await conn.ExecuteScalarAsync<int>(@"
SELECT COUNT(1)
FROM koch.AiMessages m
INNER JOIN koch.AiConversations c ON c.Id = m.ConversationId
WHERE ISNULL(c.SiteId, 0) = @SiteId
  AND m.Role = N'assistant'
  AND m.WasGrounded = 1
  AND m.CreatedAt >= DATEADD(DAY, -@Days, SYSDATETIME())",
                new { SiteId = siteId, Days = days <= 0 ? 14 : days });
        }

        public async Task<int> CountRejectedAsync(int siteId, int days)
        {
            using var conn = _factory.CreateConnection();
            return await conn.ExecuteScalarAsync<int>(@"
SELECT COUNT(1)
FROM koch.AiMessages m
INNER JOIN koch.AiConversations c ON c.Id = m.ConversationId
WHERE ISNULL(c.SiteId, 0) = @SiteId
  AND m.Role = N'assistant'
  AND m.Intent = N'OutOfScope'
  AND m.CreatedAt >= DATEADD(DAY, -@Days, SYSDATETIME())",
                new { SiteId = siteId, Days = days <= 0 ? 14 : days });
        }

        static string Trunc(string s, int n)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            s = s.Trim();
            return s.Length <= n ? s : s.Substring(0, n);
        }
    }
}
