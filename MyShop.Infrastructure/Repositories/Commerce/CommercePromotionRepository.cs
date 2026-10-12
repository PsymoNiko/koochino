using Dapper;
using MyShop.Application.Interfaces.Commerce;
using MyShop.Core.Interfaces;
using MyShop.Core.Models.Commerce;
using MyShop.Infrastructure.Data;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Infrastructure.Repositories.Commerce
{
    public class CommercePromotionRepository : ICommercePromotionRepository
    {
        private readonly IDbConnectionFactory _factory;
        public CommercePromotionRepository(IDbConnectionFactory factory) => _factory = factory;

        public async Task<List<CommercePromotion>> ListAllAsync(int? siteId)
        {
            using var conn = _factory.CreateConnection();
            var rows = await conn.QueryAsync<CommercePromotion>(@"
                SELECT * FROM koch.CommercePromotions
                WHERE (@SiteId IS NULL OR SiteId IS NULL OR SiteId = @SiteId)
                ORDER BY Id DESC",
                new { SiteId = siteId });
            return rows.ToList();
        }

        public async Task<List<CommercePromotion>> ListActiveAsync(int? siteId, int? storeId)
        {
            using var conn = _factory.CreateConnection();
            var rows = await conn.QueryAsync<CommercePromotion>(@"
                SELECT * FROM koch.CommercePromotions
                WHERE IsActive = 1
                  AND (@SiteId IS NULL OR SiteId IS NULL OR SiteId = @SiteId)
                  AND (@StoreId IS NULL OR StoreId IS NULL OR StoreId = @StoreId)
                ORDER BY Id DESC",
                new { SiteId = siteId, StoreId = storeId });
            return rows.ToList();
        }

        public async Task<CommercePromotion> GetByCodeAsync(string code, int? siteId)
        {
            if (string.IsNullOrWhiteSpace(code)) return null;
            using var conn = _factory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<CommercePromotion>(@"
                SELECT * FROM koch.CommercePromotions
                WHERE IsActive = 1 AND Code = @Code
                  AND (@SiteId IS NULL OR SiteId IS NULL OR SiteId = @SiteId)",
                new { Code = code.Trim().ToUpperInvariant(), SiteId = siteId });
        }

        public async Task<CommercePromotion> GetAsync(int id)
        {
            using var conn = _factory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<CommercePromotion>(
                "SELECT * FROM koch.CommercePromotions WHERE Id=@Id", new { Id = id });
        }

        public async Task<int> InsertAsync(CommercePromotion m)
        {
            using var conn = _factory.CreateConnection();
            return await conn.ExecuteScalarAsync<int>(@"
                INSERT INTO koch.CommercePromotions
                    (SiteId, StoreId, Name, Code, DiscountType, DiscountValue, MinSubtotal, MaxDiscount,
                     StartsAt, EndsAt, IsActive, UsageLimit)
                OUTPUT INSERTED.Id
                VALUES (@SiteId, @StoreId, @Name, @Code, @DiscountType, @DiscountValue, @MinSubtotal, @MaxDiscount,
                        @StartsAt, @EndsAt, @IsActive, @UsageLimit)", m);
        }

        public async Task UpdateAsync(CommercePromotion m)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(@"
                UPDATE koch.CommercePromotions
                SET StoreId=@StoreId, Name=@Name, Code=@Code, DiscountType=@DiscountType, DiscountValue=@DiscountValue,
                    MinSubtotal=@MinSubtotal, MaxDiscount=@MaxDiscount, StartsAt=@StartsAt, EndsAt=@EndsAt,
                    IsActive=@IsActive, UsageLimit=@UsageLimit
                WHERE Id=@Id", m);
        }

        public async Task DeleteAsync(int id)
        {
            using var conn = _factory.CreateConnection();
            await ReferenceCount.EnsureUnusedAsync(conn, new { Id = id },
                ("SELECT COUNT(1) FROM koch.CommercePromotionRedemptions WHERE PromotionId=@Id", "سفارش با این پروموشن"));
            await conn.ExecuteAsync("DELETE FROM koch.CommercePromotions WHERE Id=@Id", new { Id = id });
        }

        public async Task IncrementUsageAsync(int promotionId)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(
                "UPDATE koch.CommercePromotions SET UsedCount = UsedCount + 1 WHERE Id=@Id",
                new { Id = promotionId });
        }

        public async Task InsertRedemptionAsync(int promotionId, int orderId, int userId, long amount)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(@"
                INSERT INTO koch.CommercePromotionRedemptions (PromotionId, OrderId, UserId, Amount)
                VALUES (@PromotionId, @OrderId, @UserId, @Amount)",
                new { PromotionId = promotionId, OrderId = orderId, UserId = userId, Amount = amount });
        }
    }
}
