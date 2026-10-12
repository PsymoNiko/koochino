using Dapper;
using MyShop.Application.Interfaces.Commerce;
using MyShop.Core.Interfaces;
using MyShop.Core.Models.Commerce;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Infrastructure.Repositories.Commerce
{
    public class CommerceSettlementRepository : ICommerceSettlementRepository
    {
        private readonly IDbConnectionFactory _factory;
        public CommerceSettlementRepository(IDbConnectionFactory factory) => _factory = factory;

        public async Task<List<CommerceCommissionRate>> ListRatesAsync()
        {
            using var conn = _factory.CreateConnection();
            var rows = await conn.QueryAsync<CommerceCommissionRate>(
                "SELECT * FROM koch.CommerceCommissionRates ORDER BY StoreId, SiteId, Id");
            return rows.ToList();
        }

        public async Task UpsertRateAsync(int? siteId, int? storeId, decimal ratePercent)
        {
            using var conn = _factory.CreateConnection();
            var existing = await conn.QueryFirstOrDefaultAsync<CommerceCommissionRate>(@"
                SELECT TOP 1 * FROM koch.CommerceCommissionRates
                WHERE ((@SiteId IS NULL AND SiteId IS NULL) OR SiteId=@SiteId)
                  AND ((@StoreId IS NULL AND StoreId IS NULL) OR StoreId=@StoreId)
                ORDER BY Id",
                new { SiteId = siteId, StoreId = storeId });
            if (existing != null)
            {
                await conn.ExecuteAsync(@"
                    UPDATE koch.CommerceCommissionRates
                    SET RatePercent=@RatePercent, UpdatedAt=SYSDATETIME()
                    WHERE Id=@Id",
                    new { existing.Id, RatePercent = ratePercent });
                return;
            }
            await conn.ExecuteAsync(@"
                INSERT INTO koch.CommerceCommissionRates (SiteId, StoreId, RatePercent)
                VALUES (@SiteId, @StoreId, @RatePercent)",
                new { SiteId = siteId, StoreId = storeId, RatePercent = ratePercent });
        }

        public async Task<List<CommerceSettlement>> ListSettlementsAsync(IReadOnlyList<int> storeIds)
        {
            using var conn = _factory.CreateConnection();
            var sql = @"
                SELECT s.*, st.StoreName
                FROM koch.CommerceSettlements s
                LEFT JOIN koch.Stores st ON st.StoreID = s.StoreId
                WHERE 1=1";
            if (storeIds != null)
            {
                if (storeIds.Count == 0) return new List<CommerceSettlement>();
                sql += " AND s.StoreId IN @StoreIds";
            }
            sql += " ORDER BY s.Id DESC";
            var rows = await conn.QueryAsync<CommerceSettlement>(sql, new { StoreIds = storeIds?.ToArray() });
            return rows.ToList();
        }

        public async Task<CommerceSettlement> GetByPeriodAsync(int storeId, DateTime periodFrom, DateTime periodTo)
        {
            using var conn = _factory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<CommerceSettlement>(@"
                SELECT TOP 1 s.*, st.StoreName
                FROM koch.CommerceSettlements s
                LEFT JOIN koch.Stores st ON st.StoreID = s.StoreId
                WHERE s.StoreId=@StoreId AND s.PeriodFrom=@PeriodFrom AND s.PeriodTo=@PeriodTo
                  AND s.Status=@Settled
                ORDER BY s.Id DESC",
                new
                {
                    StoreId = storeId,
                    PeriodFrom = periodFrom,
                    PeriodTo = periodTo,
                    Settled = CommerceConstants.SettlementStatus.Settled
                });
        }

        public async Task<int> InsertAsync(CommerceSettlement row)
        {
            using var conn = _factory.CreateConnection();
            return await conn.ExecuteScalarAsync<int>(@"
                INSERT INTO koch.CommerceSettlements
                    (StoreId, PeriodFrom, PeriodTo, GrossAmount, RatePercent, CommissionAmount, NetAmount, Status, SettledByUserId, Notes)
                OUTPUT INSERTED.Id
                VALUES
                    (@StoreId, @PeriodFrom, @PeriodTo, @GrossAmount, @RatePercent, @CommissionAmount, @NetAmount, @Status, @SettledByUserId, @Notes)",
                row);
        }

        public async Task VoidAsync(int id)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(
                "UPDATE koch.CommerceSettlements SET Status=@Voided WHERE Id=@Id AND Status=@Settled",
                new
                {
                    Id = id,
                    Voided = CommerceConstants.SettlementStatus.Voided,
                    Settled = CommerceConstants.SettlementStatus.Settled
                });
        }
    }
}
