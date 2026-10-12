using Dapper;
using MyShop.Application.Interfaces.Loyalty;
using MyShop.Core.Interfaces;
using MyShop.Core.Models.Loyalty;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Infrastructure.Repositories.Loyalty
{
    public class LoyaltyRoadmapRepository : ILoyaltyRoadmapRepository
    {
        private readonly IDbConnectionFactory _db;
        public LoyaltyRoadmapRepository(IDbConnectionFactory db) => _db = db;

        public async Task<List<SmsCampaign>> ListCampaignsAsync(int? siteId, int top = 100)
        {
            using var c = _db.CreateConnection();
            return (await c.QueryAsync<SmsCampaign>(@"
SELECT TOP (@top) * FROM koch.SmsCampaigns
WHERE (@siteId IS NULL OR SiteId = @siteId OR SiteId IS NULL)
ORDER BY Id DESC", new { top, siteId })).ToList();
        }

        public async Task<SmsCampaign> GetCampaignAsync(int id)
        {
            using var c = _db.CreateConnection();
            return await c.QueryFirstOrDefaultAsync<SmsCampaign>("SELECT * FROM koch.SmsCampaigns WHERE Id=@id", new { id });
        }

        public async Task<int> UpsertCampaignAsync(SmsCampaign camp)
        {
            using var c = _db.CreateConnection();
            if (camp.Id > 0)
            {
                await c.ExecuteAsync(@"UPDATE koch.SmsCampaigns SET Title=@Title, MessageText=@MessageText, AudienceType=@AudienceType,
SegmentId=@SegmentId, ManualPhones=@ManualPhones, Status=@Status, ScheduledAt=@ScheduledAt, SiteId=@SiteId WHERE Id=@Id", camp);
                return camp.Id;
            }
            return await c.ExecuteScalarAsync<int>(@"INSERT INTO koch.SmsCampaigns (SiteId, Title, MessageText, AudienceType, SegmentId, ManualPhones, Status, ScheduledAt, CreatedByUserId)
VALUES (@SiteId, @Title, @MessageText, @AudienceType, @SegmentId, @ManualPhones, @Status, @ScheduledAt, @CreatedByUserId);
SELECT CAST(SCOPE_IDENTITY() AS INT);", camp);
        }

        public async Task UpdateCampaignStatsAsync(int id, string status, int sent, int fail)
        {
            using var c = _db.CreateConnection();
            await c.ExecuteAsync(@"UPDATE koch.SmsCampaigns SET Status=@status, SentCount=@sent, FailCount=@fail, SentAt=SYSDATETIME() WHERE Id=@id",
                new { id, status, sent, fail });
        }

        public async Task<List<CustomerSegment>> ListSegmentsAsync(int? siteId)
        {
            using var c = _db.CreateConnection();
            return (await c.QueryAsync<CustomerSegment>(@"
SELECT * FROM koch.CustomerSegments WHERE (@siteId IS NULL OR SiteId=@siteId OR SiteId IS NULL) ORDER BY Id DESC", new { siteId })).ToList();
        }

        public async Task<int> UpsertSegmentAsync(CustomerSegment s)
        {
            using var c = _db.CreateConnection();
            if (s.Id > 0)
            {
                await c.ExecuteAsync(@"UPDATE koch.CustomerSegments SET Name=@Name, Description=@Description, RuleJson=@RuleJson, IsActive=@IsActive, SiteId=@SiteId WHERE Id=@Id", s);
                return s.Id;
            }
            return await c.ExecuteScalarAsync<int>(@"INSERT INTO koch.CustomerSegments (SiteId, Name, Description, RuleJson, IsActive)
VALUES (@SiteId, @Name, @Description, @RuleJson, @IsActive); SELECT CAST(SCOPE_IDENTITY() AS INT);", s);
        }

        public async Task DeleteSegmentAsync(int id)
        {
            using var c = _db.CreateConnection();
            await c.ExecuteAsync("DELETE FROM koch.CustomerSegments WHERE Id=@id", new { id });
        }

        public async Task UpsertRfmAsync(CustomerRfmScore score)
        {
            using var c = _db.CreateConnection();
            await c.ExecuteAsync(@"
MERGE koch.CustomerRfmScores AS t
USING (SELECT @UserId AS UserId) AS s ON t.UserId = s.UserId
WHEN MATCHED THEN UPDATE SET SiteId=@SiteId, RecencyScore=@RecencyScore, FrequencyScore=@FrequencyScore, MonetaryScore=@MonetaryScore,
    LastOrderAt=@LastOrderAt, OrderCount=@OrderCount, TotalSpend=@TotalSpend, UpdatedAt=SYSDATETIME()
WHEN NOT MATCHED THEN INSERT (UserId, SiteId, RecencyScore, FrequencyScore, MonetaryScore, LastOrderAt, OrderCount, TotalSpend)
VALUES (@UserId, @SiteId, @RecencyScore, @FrequencyScore, @MonetaryScore, @LastOrderAt, @OrderCount, @TotalSpend);", score);
        }

        public async Task<List<CustomerRfmScore>> ListRfmAsync(int? siteId, int top = 500)
        {
            using var c = _db.CreateConnection();
            return (await c.QueryAsync<CustomerRfmScore>(@"
SELECT TOP (@top) r.*, u.PhoneNumber, (ISNULL(u.FirstName,'') + N' ' + ISNULL(u.LastName,'')) AS DisplayName
FROM koch.CustomerRfmScores r
INNER JOIN koch.Users u ON u.Id = r.UserId
WHERE (@siteId IS NULL OR r.SiteId=@siteId OR r.SiteId IS NULL)
ORDER BY r.UpdatedAt DESC", new { top, siteId })).ToList();
        }

        public async Task<List<VipTier>> ListVipTiersAsync(int? siteId)
        {
            using var c = _db.CreateConnection();
            return (await c.QueryAsync<VipTier>(@"
SELECT * FROM koch.VipTiers WHERE IsActive=1 AND (@siteId IS NULL OR SiteId=@siteId OR SiteId IS NULL)
ORDER BY MinPoints ASC", new { siteId })).ToList();
        }

        public async Task<int> UpsertVipTierAsync(VipTier t)
        {
            using var c = _db.CreateConnection();
            if (t.Id > 0)
            {
                await c.ExecuteAsync(@"UPDATE koch.VipTiers SET Name=@Name, MinPoints=@MinPoints, SortOrder=@SortOrder, ColorHex=@ColorHex, BenefitsJson=@BenefitsJson, IsActive=@IsActive, SiteId=@SiteId WHERE Id=@Id", t);
                return t.Id;
            }
            return await c.ExecuteScalarAsync<int>(@"INSERT INTO koch.VipTiers (SiteId, Name, MinPoints, SortOrder, ColorHex, BenefitsJson, IsActive)
VALUES (@SiteId, @Name, @MinPoints, @SortOrder, @ColorHex, @BenefitsJson, @IsActive); SELECT CAST(SCOPE_IDENTITY() AS INT);", t);
        }

        public async Task DeleteVipTierAsync(int id)
        {
            using var c = _db.CreateConnection();
            await c.ExecuteAsync("UPDATE koch.VipTiers SET IsActive=0 WHERE Id=@id", new { id });
        }

        public async Task<decimal> GetWalletBalanceAsync(int userId)
        {
            using var c = _db.CreateConnection();
            return await c.ExecuteScalarAsync<decimal?>("SELECT Balance FROM koch.UserWallets WHERE UserId=@userId", new { userId }) ?? 0m;
        }

        public async Task<List<WalletTransaction>> ListWalletTxAsync(int userId, int top = 50)
        {
            using var c = _db.CreateConnection();
            return (await c.QueryAsync<WalletTransaction>(@"SELECT TOP (@top) * FROM koch.WalletTransactions WHERE UserId=@userId ORDER BY Id DESC", new { userId, top })).ToList();
        }

        public async Task AdjustWalletAsync(int userId, decimal amount, string txType, string reason, int? byUserId, string referenceId)
        {
            using var c = _db.CreateConnection();
            c.Open();
            using var tx = c.BeginTransaction();
            await c.ExecuteAsync(@"
MERGE koch.UserWallets AS t USING (SELECT @userId AS UserId) AS s ON t.UserId=s.UserId
WHEN MATCHED THEN UPDATE SET Balance = Balance + @amount, UpdatedAt=SYSDATETIME()
WHEN NOT MATCHED THEN INSERT (UserId, Balance) VALUES (@userId, @amount);", new { userId, amount }, tx);
            await c.ExecuteAsync(@"INSERT INTO koch.WalletTransactions (UserId, Amount, TxType, Reason, ReferenceId, CreatedByUserId)
VALUES (@userId, @amount, @txType, @reason, @referenceId, @byUserId)", new { userId, amount, txType, reason, referenceId, byUserId }, tx);
            tx.Commit();
        }

        public async Task InsertNpsAsync(NpsResponse r)
        {
            using var c = _db.CreateConnection();
            await c.ExecuteAsync(@"INSERT INTO koch.NpsResponses (UserId, OrderId, Score, Comment, SiteId) VALUES (@UserId, @OrderId, @Score, @Comment, @SiteId)", r);
        }

        public async Task<List<NpsResponse>> ListNpsAsync(int? siteId, int top = 200)
        {
            using var c = _db.CreateConnection();
            return (await c.QueryAsync<NpsResponse>(@"SELECT TOP (@top) * FROM koch.NpsResponses WHERE (@siteId IS NULL OR SiteId=@siteId) ORDER BY Id DESC", new { top, siteId })).ToList();
        }

        public async Task<List<LoyaltyAutomation>> ListAutomationsAsync(int? siteId)
        {
            using var c = _db.CreateConnection();
            return (await c.QueryAsync<LoyaltyAutomation>(@"SELECT * FROM koch.LoyaltyAutomations WHERE (@siteId IS NULL OR SiteId=@siteId OR SiteId IS NULL) ORDER BY Id DESC", new { siteId })).ToList();
        }

        public async Task<int> UpsertAutomationAsync(LoyaltyAutomation a)
        {
            using var c = _db.CreateConnection();
            if (a.Id > 0)
            {
                await c.ExecuteAsync(@"UPDATE koch.LoyaltyAutomations SET Name=@Name, TriggerType=@TriggerType, TriggerValue=@TriggerValue, ActionType=@ActionType, ActionPayload=@ActionPayload, IsActive=@IsActive, SiteId=@SiteId WHERE Id=@Id", a);
                return a.Id;
            }
            return await c.ExecuteScalarAsync<int>(@"INSERT INTO koch.LoyaltyAutomations (SiteId, Name, TriggerType, TriggerValue, ActionType, ActionPayload, IsActive)
VALUES (@SiteId, @Name, @TriggerType, @TriggerValue, @ActionType, @ActionPayload, @IsActive); SELECT CAST(SCOPE_IDENTITY() AS INT);", a);
        }

        public async Task DeleteAutomationAsync(int id)
        {
            using var c = _db.CreateConnection();
            await c.ExecuteAsync("DELETE FROM koch.LoyaltyAutomations WHERE Id=@id", new { id });
        }

        public async Task SetUserBirthDateAsync(int userId, DateTime? birthDate)
        {
            using var c = _db.CreateConnection();
            await c.ExecuteAsync("UPDATE koch.Users SET BirthDate=@birthDate WHERE Id=@userId", new { userId, birthDate });
        }

        public async Task<DateTime?> GetUserBirthDateAsync(int userId)
        {
            using var c = _db.CreateConnection();
            return await c.ExecuteScalarAsync<DateTime?>("SELECT BirthDate FROM koch.Users WHERE Id=@userId", new { userId });
        }

        public async Task<List<(int UserId, string Phone, DateTime? BirthDate)>> ListUsersForAudienceAsync(int? siteId, int top = 5000)
        {
            using var c = _db.CreateConnection();
            var rows = await c.QueryAsync<(int UserId, string Phone, DateTime? BirthDate)>(@"
SELECT TOP (@top) Id AS UserId, PhoneNumber AS Phone, BirthDate
FROM koch.Users WHERE IsActive=1 AND PhoneNumber IS NOT NULL AND LEN(PhoneNumber)>=10
AND (@siteId IS NULL OR SiteId=@siteId)", new { top, siteId });
            return rows.ToList();
        }
    }
}
