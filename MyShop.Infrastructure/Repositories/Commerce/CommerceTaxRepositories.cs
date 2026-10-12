using Dapper;
using MyShop.Application.Interfaces.Commerce;
using MyShop.Core.Interfaces;
using MyShop.Core.Models.Commerce;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Infrastructure.Repositories.Commerce
{
    public class CommerceTaxRepository : ICommerceTaxRepository
    {
        private readonly IDbConnectionFactory _factory;
        public CommerceTaxRepository(IDbConnectionFactory factory) => _factory = factory;

        public async Task<List<CommerceTaxRate>> ListAsync(int? siteId)
        {
            using var conn = _factory.CreateConnection();
            var rows = await conn.QueryAsync<CommerceTaxRate>(@"
                SELECT * FROM koch.CommerceTaxRates
                WHERE (@SiteId IS NULL OR SiteId IS NULL OR SiteId = @SiteId)
                ORDER BY Id DESC", new { SiteId = siteId });
            return rows.ToList();
        }

        public async Task<CommerceTaxRate> GetActiveForSiteAsync(int? siteId)
        {
            using var conn = _factory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<CommerceTaxRate>(@"
                SELECT TOP 1 * FROM koch.CommerceTaxRates
                WHERE IsActive = 1
                  AND (@SiteId IS NULL OR SiteId IS NULL OR SiteId = @SiteId)
                ORDER BY CASE WHEN SiteId = @SiteId THEN 0 ELSE 1 END, Id DESC",
                new { SiteId = siteId });
        }

        public async Task<int> UpsertAsync(CommerceTaxRate m)
        {
            using var conn = _factory.CreateConnection();
            if (m.Id > 0)
            {
                await conn.ExecuteAsync(@"
                    UPDATE koch.CommerceTaxRates
                    SET Name=@Name, RatePercent=@RatePercent, AppliesToShipping=@AppliesToShipping, IsActive=@IsActive
                    WHERE Id=@Id", m);
                return m.Id;
            }
            return await conn.ExecuteScalarAsync<int>(@"
                INSERT INTO koch.CommerceTaxRates (SiteId, Name, RatePercent, AppliesToShipping, IsActive)
                OUTPUT INSERTED.Id
                VALUES (@SiteId, @Name, @RatePercent, @AppliesToShipping, @IsActive)", m);
        }

        public async Task DeleteAsync(int id)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync("DELETE FROM koch.CommerceTaxRates WHERE Id=@Id", new { Id = id });
        }
    }

    public class CommerceStoreShippingRepository : ICommerceStoreShippingRepository
    {
        private readonly IDbConnectionFactory _factory;
        public CommerceStoreShippingRepository(IDbConnectionFactory factory) => _factory = factory;

        public async Task<List<CommerceStoreShippingRate>> ListByMethodAsync(int shippingMethodId)
        {
            using var conn = _factory.CreateConnection();
            var rows = await conn.QueryAsync<CommerceStoreShippingRate>(
                "SELECT * FROM koch.CommerceStoreShippingRates WHERE ShippingMethodId=@Id",
                new { Id = shippingMethodId });
            return rows.ToList();
        }

        public async Task UpsertAsync(CommerceStoreShippingRate m)
        {
            using var conn = _factory.CreateConnection();
            if (m.Id > 0)
            {
                await conn.ExecuteAsync(
                    "UPDATE koch.CommerceStoreShippingRates SET StoreId=@StoreId, Fee=@Fee WHERE Id=@Id", m);
                return;
            }
            await conn.ExecuteAsync(@"
                IF EXISTS (SELECT 1 FROM koch.CommerceStoreShippingRates WHERE ShippingMethodId=@ShippingMethodId AND StoreId=@StoreId)
                    UPDATE koch.CommerceStoreShippingRates SET Fee=@Fee WHERE ShippingMethodId=@ShippingMethodId AND StoreId=@StoreId
                ELSE
                    INSERT INTO koch.CommerceStoreShippingRates (ShippingMethodId, StoreId, Fee)
                    VALUES (@ShippingMethodId, @StoreId, @Fee)", m);
        }

        public async Task DeleteAsync(int id)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync("DELETE FROM koch.CommerceStoreShippingRates WHERE Id=@Id", new { Id = id });
        }
    }
}
