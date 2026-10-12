using Dapper;
using MyShop.Application.Interfaces.Ai;
using MyShop.Core.Interfaces;
using MyShop.Core.Models.Ai;
using MyShop.Core.Models.SupportChat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Infrastructure.Repositories.Ai
{
    /// <summary>
    /// Watermark and closed-chat queue for the knowledge-base draft learner. The watermark is the later of
    /// chat close time and CSAT time so a rating that arrives after close is still picked up.
    /// </summary>
    public class AiKbLearnRepository : IAiKbLearnRepository
    {
        const string ThreadSelect = @"
SELECT c.Id, c.SiteId, c.UserId, c.GuestToken, c.VisitorName, c.VisitorPhone, c.Audience, c.Status,
       ISNULL(c.Priority, N'Normal') AS Priority, ISNULL(c.Channel, N'Website') AS Channel,
       c.AssignedAdminUserId, c.TeamId, t.Name AS TeamName,
       c.LastMessageAt, c.LastMessagePreview, c.UnreadByAdmin, c.UnreadByUser, c.CreatedAt,
       ISNULL(c.UpdatedAt, c.CreatedAt) AS UpdatedAt,
       CASE WHEN r.CreatedAt > ISNULL(c.ClosedAt, c.UpdatedAt) THEN r.CreatedAt ELSE ISNULL(c.ClosedAt, c.UpdatedAt) END AS ClosedAt,
       c.CurrentPageUrl, c.ExternalThreadId,
       LTRIM(RTRIM(ISNULL(u.FirstName,N'') + N' ' + ISNULL(u.LastName,N''))) AS UserDisplayName,
       u.PhoneNumber AS UserPhone,
       LTRIM(RTRIM(ISNULL(a.FirstName,N'') + N' ' + ISNULL(a.LastName,N''))) AS AssignedAdminName
FROM koch.SupportChats c
INNER JOIN koch.SupportCsatRatings r ON r.ChatId=c.Id AND r.Score >= @MinCsat
LEFT JOIN koch.Users u ON u.Id = c.UserId
LEFT JOIN koch.Users a ON a.Id = c.AssignedAdminUserId
LEFT JOIN koch.SupportTeams t ON t.Id = c.TeamId";

        private readonly IDbConnectionFactory _factory;
        public AiKbLearnRepository(IDbConnectionFactory factory) => _factory = factory;

        public async Task<AiKbLearnCursor> GetCursorAsync(int siteId)
        {
            using var conn = _factory.CreateConnection();
            var row = await conn.QueryFirstOrDefaultAsync<AiKbLearnCursor>(
                "SELECT SiteId, LastClosedAt, LastRunAt, LastResult FROM koch.AiKbLearnCursors WHERE SiteId=@SiteId",
                new { SiteId = siteId });
            return row ?? new AiKbLearnCursor { SiteId = siteId };
        }

        public async Task SaveCursorAsync(int siteId, DateTime? lastClosedAt, string result)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(@"
MERGE koch.AiKbLearnCursors AS t
USING (SELECT @SiteId AS SiteId) AS s ON t.SiteId=s.SiteId
WHEN MATCHED THEN UPDATE SET
    LastClosedAt=@LastClosedAt,
    LastRunAt=SYSDATETIME(),
    LastResult=@LastResult
WHEN NOT MATCHED THEN INSERT (SiteId, LastClosedAt, LastRunAt, LastResult)
VALUES (@SiteId, @LastClosedAt, SYSDATETIME(), @LastResult);",
                new { SiteId = siteId, LastClosedAt = lastClosedAt, LastResult = Trunc(result, 400) });
        }

        public async Task<List<int>> ListEnabledSiteIdsAsync()
        {
            using var conn = _factory.CreateConnection();
            var rows = await conn.QueryAsync<int>(@"
SELECT SiteId FROM koch.SupportSettings
WHERE AiLearnFromAdminChat=1 AND SiteId>0
ORDER BY SiteId");
            return rows?.ToList() ?? new List<int>();
        }

        public async Task<List<SupportChatThread>> ListClosedAfterAsync(int? siteId, DateTime? afterClosedAt, int take)
        {
            using var conn = _factory.CreateConnection();
            var rows = await conn.QueryAsync<SupportChatThread>(
                ThreadSelect + @"
WHERE c.Status=N'Closed'
  AND (@SiteId IS NULL OR @SiteId=0 OR c.SiteId=@SiteId)
  AND (
        @AfterClosedAt IS NULL
        OR CASE WHEN r.CreatedAt > ISNULL(c.ClosedAt, c.UpdatedAt) THEN r.CreatedAt ELSE ISNULL(c.ClosedAt, c.UpdatedAt) END > @AfterClosedAt
      )
ORDER BY CASE WHEN r.CreatedAt > ISNULL(c.ClosedAt, c.UpdatedAt) THEN r.CreatedAt ELSE ISNULL(c.ClosedAt, c.UpdatedAt) END, c.Id
OFFSET 0 ROWS FETCH NEXT @Take ROWS ONLY",
                new
                {
                    SiteId = siteId,
                    AfterClosedAt = afterClosedAt,
                    MinCsat = (int)AiLearnDefaults.MinCsatScore,
                    Take = take <= 0 ? AiLearnDefaults.ChatsPerRun : take
                });
            return rows?.ToList() ?? new List<SupportChatThread>();
        }

        static string Trunc(string s, int n)
        {
            if (string.IsNullOrEmpty(s)) return s;
            s = s.Trim();
            return s.Length <= n ? s : s.Substring(0, n);
        }
    }
}
