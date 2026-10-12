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
    public class AiUsageRepository : IAiUsageRepository
    {
        private readonly IDbConnectionFactory _factory;
        public AiUsageRepository(IDbConnectionFactory factory) => _factory = factory;

        public async Task<AiUsageSnapshot> GetSnapshotAsync(int siteId, DateTime day, string userKey, string guestKey, string ipKey)
        {
            using var conn = _factory.CreateConnection();
            var row = await conn.QueryFirstOrDefaultAsync<AiUsageSnapshot>(@"
SELECT
  ISNULL((SELECT SUM(Messages) FROM koch.AiUsageCounters
          WHERE SiteId=@SiteId AND UsageDate=@Day AND Scope=N'User'  AND ScopeKey=@UserKey), 0)  AS UserMessages,
  ISNULL((SELECT SUM(Messages) FROM koch.AiUsageCounters
          WHERE SiteId=@SiteId AND UsageDate=@Day AND Scope=N'Guest' AND ScopeKey=@GuestKey), 0) AS GuestMessages,
  ISNULL((SELECT SUM(Messages) FROM koch.AiUsageCounters
          WHERE SiteId=@SiteId AND UsageDate=@Day AND Scope=N'Ip'    AND ScopeKey=@IpKey), 0)    AS IpMessages,
  ISNULL((SELECT SUM(PromptTokens + CompletionTokens) FROM koch.AiUsageCounters
          WHERE SiteId=@SiteId AND UsageDate=@Day), 0)                                           AS SiteTokens",
                new
                {
                    SiteId = siteId,
                    Day = day.Date,
                    UserKey = userKey ?? "\u0000",
                    GuestKey = guestKey ?? "\u0000",
                    IpKey = ipKey ?? "\u0000"
                });
            return row ?? new AiUsageSnapshot();
        }

        public async Task IncrementAsync(int siteId, DateTime day, string scope, string scopeKey, int messages, int promptTokens, int completionTokens)
        {
            if (string.IsNullOrWhiteSpace(scope) || string.IsNullOrWhiteSpace(scopeKey)) return;
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(@"
MERGE koch.AiUsageCounters WITH (HOLDLOCK) AS t
USING (SELECT @SiteId AS SiteId, @Day AS UsageDate, @Scope AS Scope, @ScopeKey AS ScopeKey) AS s
    ON t.SiteId=s.SiteId AND t.UsageDate=s.UsageDate AND t.Scope=s.Scope AND t.ScopeKey=s.ScopeKey
WHEN MATCHED THEN UPDATE SET
    Messages = t.Messages + @Messages,
    PromptTokens = t.PromptTokens + @PromptTokens,
    CompletionTokens = t.CompletionTokens + @CompletionTokens,
    UpdatedAt = SYSDATETIME()
WHEN NOT MATCHED THEN INSERT (SiteId, UsageDate, Scope, ScopeKey, Messages, PromptTokens, CompletionTokens, UpdatedAt)
    VALUES (@SiteId, @Day, @Scope, @ScopeKey, @Messages, @PromptTokens, @CompletionTokens, SYSDATETIME());",
                new
                {
                    SiteId = siteId,
                    Day = day.Date,
                    Scope = scope,
                    ScopeKey = scopeKey.Length <= 128 ? scopeKey : scopeKey.Substring(0, 128),
                    Messages = messages,
                    PromptTokens = promptTokens,
                    CompletionTokens = completionTokens
                });
        }

        public async Task<List<AiUsageDailyRow>> GetDailyAsync(int siteId, int days)
        {
            using var conn = _factory.CreateConnection();
            var rows = await conn.QueryAsync<AiUsageDailyRow>(@"
SELECT UsageDate,
       SUM(Messages) AS Messages,
       SUM(PromptTokens) AS PromptTokens,
       SUM(CompletionTokens) AS CompletionTokens
FROM koch.AiUsageCounters
WHERE SiteId=@SiteId AND UsageDate >= DATEADD(DAY, -@Days, CAST(SYSDATETIME() AS DATE))
GROUP BY UsageDate
ORDER BY UsageDate DESC", new { SiteId = siteId, Days = days <= 0 ? 14 : days });
            return rows?.ToList() ?? new List<AiUsageDailyRow>();
        }
    }
}
