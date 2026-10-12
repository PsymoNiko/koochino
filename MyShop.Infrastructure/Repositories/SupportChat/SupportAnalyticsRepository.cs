using Dapper;
using MyShop.Application.Interfaces.SupportChat;
using MyShop.Core.Interfaces;
using MyShop.Core.Models.SupportChat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Infrastructure.Repositories.SupportChat
{
    public class SupportAnalyticsRepository : ISupportAnalyticsRepository
    {
        private readonly IDbConnectionFactory _factory;
        public SupportAnalyticsRepository(IDbConnectionFactory factory) => _factory = factory;

        public async Task MarkFirstResponseAsync(Guid chatId)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(@"
UPDATE koch.SupportChats
SET FirstResponseAt = ISNULL(FirstResponseAt, SYSDATETIME())
WHERE Id=@Id", new { Id = chatId });
        }

        public async Task UpsertCsatAsync(SupportCsatRating rating)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(@"
MERGE koch.SupportCsatRatings AS t
USING (SELECT @ChatId AS ChatId) AS s ON t.ChatId=s.ChatId
WHEN MATCHED THEN UPDATE SET Score=@Score, Comment=@Comment, RatedByUserId=@RatedByUserId, GuestToken=@GuestToken
WHEN NOT MATCHED THEN INSERT (ChatId, SiteId, Score, Comment, RatedByUserId, GuestToken)
VALUES (@ChatId, @SiteId, @Score, @Comment, @RatedByUserId, @GuestToken);", rating);
        }

        public async Task<SupportCsatRating> GetCsatAsync(Guid chatId)
        {
            using var conn = _factory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<SupportCsatRating>(
                "SELECT * FROM koch.SupportCsatRatings WHERE ChatId=@ChatId", new { ChatId = chatId });
        }

        public async Task<SupportAnalyticsSummary> GetSummaryAsync(SupportAnalyticsFilter filter)
        {
            Normalize(filter);
            using var conn = _factory.CreateConnection();
            var row = await conn.QueryFirstOrDefaultAsync<SupportAnalyticsSummary>(@"
SELECT
  COUNT(1) AS TotalChats,
  SUM(CASE WHEN c.Status=N'Waiting' THEN 1 ELSE 0 END) AS WaitingChats,
  SUM(CASE WHEN c.Status=N'Active' THEN 1 ELSE 0 END) AS ActiveChats,
  SUM(CASE WHEN c.Status=N'Closed' THEN 1 ELSE 0 END) AS ClosedChats,
  AVG(CASE WHEN c.FirstResponseAt IS NOT NULL THEN CAST(DATEDIFF(SECOND, c.CreatedAt, c.FirstResponseAt) AS FLOAT)/60.0 END) AS AvgFirstResponseMinutes,
  AVG(CASE WHEN c.ClosedAt IS NOT NULL THEN CAST(DATEDIFF(SECOND, c.CreatedAt, c.ClosedAt) AS FLOAT)/60.0 END) AS AvgResolutionMinutes,
  (SELECT AVG(CAST(r.Score AS FLOAT)) FROM koch.SupportCsatRatings r
     INNER JOIN koch.SupportChats xc ON xc.Id=r.ChatId
     WHERE (@SiteId IS NULL OR @SiteId=0 OR xc.SiteId=@SiteId)
       AND r.CreatedAt>=@From AND r.CreatedAt<@ToExclusive) AS AvgCsat,
  (SELECT COUNT(1) FROM koch.SupportCsatRatings r
     INNER JOIN koch.SupportChats xc ON xc.Id=r.ChatId
     WHERE (@SiteId IS NULL OR @SiteId=0 OR xc.SiteId=@SiteId)
       AND r.CreatedAt>=@From AND r.CreatedAt<@ToExclusive) AS CsatCount,
  (SELECT COUNT(1) FROM koch.SupportChatMessages m
     INNER JOIN koch.SupportChats xc ON xc.Id=m.ChatId
     WHERE m.SenderType=N'User' AND (@SiteId IS NULL OR @SiteId=0 OR xc.SiteId=@SiteId)
       AND m.CreatedAt>=@From AND m.CreatedAt<@ToExclusive) AS MessagesFromUsers,
  (SELECT COUNT(1) FROM koch.SupportChatMessages m
     INNER JOIN koch.SupportChats xc ON xc.Id=m.ChatId
     WHERE m.SenderType=N'Admin' AND (@SiteId IS NULL OR @SiteId=0 OR xc.SiteId=@SiteId)
       AND m.CreatedAt>=@From AND m.CreatedAt<@ToExclusive) AS MessagesFromAdmins
FROM koch.SupportChats c
WHERE (@SiteId IS NULL OR @SiteId=0 OR c.SiteId=@SiteId)
  AND c.CreatedAt>=@From AND c.CreatedAt<@ToExclusive
  AND (@OperatorUserId IS NULL OR c.AssignedAdminUserId=@OperatorUserId)",
                new { filter.SiteId, filter.From, ToExclusive = filter.To.Date.AddDays(1), filter.OperatorUserId });
            return row ?? new SupportAnalyticsSummary();
        }

        public async Task<List<SupportDailyStat>> GetDailyAsync(SupportAnalyticsFilter filter)
        {
            Normalize(filter);
            using var conn = _factory.CreateConnection();
            var rows = await conn.QueryAsync<SupportDailyStat>(@"
;WITH Days AS (
  SELECT CAST(c.CreatedAt AS DATE) AS Day, COUNT(1) AS ChatsStarted,
         SUM(CASE WHEN c.Status=N'Closed' AND c.ClosedAt IS NOT NULL AND CAST(c.ClosedAt AS DATE)=CAST(c.CreatedAt AS DATE) THEN 1 ELSE 0 END) AS ChatsClosedSameDay
  FROM koch.SupportChats c
  WHERE (@SiteId IS NULL OR @SiteId=0 OR c.SiteId=@SiteId)
    AND c.CreatedAt>=@From AND c.CreatedAt<@ToExclusive
    AND (@OperatorUserId IS NULL OR c.AssignedAdminUserId=@OperatorUserId)
  GROUP BY CAST(c.CreatedAt AS DATE)
),
Closed AS (
  SELECT CAST(c.ClosedAt AS DATE) AS Day, COUNT(1) AS ChatsClosed
  FROM koch.SupportChats c
  WHERE c.ClosedAt IS NOT NULL
    AND (@SiteId IS NULL OR @SiteId=0 OR c.SiteId=@SiteId)
    AND c.ClosedAt>=@From AND c.ClosedAt<@ToExclusive
    AND (@OperatorUserId IS NULL OR c.AssignedAdminUserId=@OperatorUserId)
  GROUP BY CAST(c.ClosedAt AS DATE)
),
Csat AS (
  SELECT CAST(r.CreatedAt AS DATE) AS Day, COUNT(1) AS CsatCount, AVG(CAST(r.Score AS FLOAT)) AS AvgCsat
  FROM koch.SupportCsatRatings r
  INNER JOIN koch.SupportChats c ON c.Id=r.ChatId
  WHERE (@SiteId IS NULL OR @SiteId=0 OR c.SiteId=@SiteId)
    AND r.CreatedAt>=@From AND r.CreatedAt<@ToExclusive
    AND (@OperatorUserId IS NULL OR c.AssignedAdminUserId=@OperatorUserId)
  GROUP BY CAST(r.CreatedAt AS DATE)
)
SELECT COALESCE(d.Day, cl.Day, cs.Day) AS Day,
       ISNULL(d.ChatsStarted, 0) AS ChatsStarted,
       ISNULL(cl.ChatsClosed, 0) AS ChatsClosed,
       ISNULL(cs.CsatCount, 0) AS CsatCount,
       cs.AvgCsat
FROM Days d
FULL OUTER JOIN Closed cl ON cl.Day=d.Day
FULL OUTER JOIN Csat cs ON cs.Day=COALESCE(d.Day, cl.Day)
ORDER BY Day",
                new { filter.SiteId, filter.From, ToExclusive = filter.To.Date.AddDays(1), filter.OperatorUserId });
            return rows?.ToList() ?? new List<SupportDailyStat>();
        }

        public async Task<List<SupportOperatorStat>> GetOperatorStatsAsync(SupportAnalyticsFilter filter)
        {
            Normalize(filter);
            using var conn = _factory.CreateConnection();
            var rows = await conn.QueryAsync<SupportOperatorStat>(@"
SELECT
  c.AssignedAdminUserId AS UserId,
  LTRIM(RTRIM(ISNULL(u.FirstName,N'') + N' ' + ISNULL(u.LastName,N''))) AS DisplayName,
  COUNT(1) AS AssignedChats,
  SUM(CASE WHEN c.Status=N'Closed' THEN 1 ELSE 0 END) AS ClosedChats,
  (SELECT COUNT(1) FROM koch.SupportChatMessages m
     INNER JOIN koch.SupportChats xc ON xc.Id=m.ChatId
     WHERE m.SenderType=N'Admin' AND m.SenderUserId=c.AssignedAdminUserId
       AND (@SiteId IS NULL OR @SiteId=0 OR xc.SiteId=@SiteId)
       AND m.CreatedAt>=@From AND m.CreatedAt<@ToExclusive) AS MessagesSent,
  AVG(CASE WHEN c.FirstResponseAt IS NOT NULL THEN CAST(DATEDIFF(SECOND, c.CreatedAt, c.FirstResponseAt) AS FLOAT)/60.0 END) AS AvgFirstResponseMinutes,
  AVG(CASE WHEN c.ClosedAt IS NOT NULL THEN CAST(DATEDIFF(SECOND, c.CreatedAt, c.ClosedAt) AS FLOAT)/60.0 END) AS AvgResolutionMinutes,
  AVG(CAST(r.Score AS FLOAT)) AS AvgCsat,
  COUNT(r.Id) AS CsatCount
FROM koch.SupportChats c
LEFT JOIN koch.Users u ON u.Id=c.AssignedAdminUserId
LEFT JOIN koch.SupportCsatRatings r ON r.ChatId=c.Id
WHERE c.AssignedAdminUserId IS NOT NULL
  AND (@SiteId IS NULL OR @SiteId=0 OR c.SiteId=@SiteId)
  AND c.CreatedAt>=@From AND c.CreatedAt<@ToExclusive
  AND (@OperatorUserId IS NULL OR c.AssignedAdminUserId=@OperatorUserId)
GROUP BY c.AssignedAdminUserId, u.FirstName, u.LastName
ORDER BY ClosedChats DESC, AssignedChats DESC",
                new { filter.SiteId, filter.From, ToExclusive = filter.To.Date.AddDays(1), filter.OperatorUserId });
            return rows?.ToList() ?? new List<SupportOperatorStat>();
        }

        static void Normalize(SupportAnalyticsFilter filter)
        {
            filter ??= new SupportAnalyticsFilter();
            if (filter.From == default) filter.From = DateTime.Today.AddDays(-30);
            if (filter.To == default) filter.To = DateTime.Today;
            if (filter.To < filter.From) filter.To = filter.From;
        }
    }
}
