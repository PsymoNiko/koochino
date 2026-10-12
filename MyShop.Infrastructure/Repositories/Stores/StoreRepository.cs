using Dapper;
using MyShop.Application.Interfaces;
using MyShop.Core.Helpers;
using MyShop.Core.Interfaces;
using MyShop.Core.Models;
using MyShop.Core.Models.Security;
using MyShop.Core.Security;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Infrastructure.Data
{
    public class StoreRepository : IStoreRepository
    {
        private readonly IDbConnectionFactory _factory;
        private readonly ICurrentTenant _tenant;

        public StoreRepository(IDbConnectionFactory factory, ICurrentTenant tenant = null)
        {
            _factory = factory;
            _tenant = tenant;
        }

        public async Task<int> CountAsync()
        {
            using var conn = _factory.CreateConnection();
            var siteId = SiteScope.CurrentSiteId(_tenant);
            if (siteId.HasValue)
            {
                try
                {
                    return await conn.ExecuteScalarAsync<int>(
                        "SELECT COUNT(1) FROM koch.Stores WHERE SiteId IS NULL OR SiteId = @SiteId",
                        new { SiteId = siteId.Value });
                }
                catch { /* fallback */ }
            }

            return await conn.ExecuteScalarAsync<int>(
                "koch.spStores_Count",
                commandType: CommandType.StoredProcedure);
        }
        //اخرین مغازه داران 
        public async Task<List<Store>> LatestAsync(int count)
        {
            using var conn = _factory.CreateConnection();
            var list = (await conn.QueryAsync<Store>(
                "koch.spStores_Latest",
                new { Count = count },
                commandType: CommandType.StoredProcedure
            )).ToList();
            return await FilterStoresBySiteAsync(conn, list);
        }

        public async Task<List<Store>> TopSellingAsync(int count)
        {
            using var conn = _factory.CreateConnection();
            var list = (await conn.QueryAsync<Store>(
                "koch.spStores_TopSelling",
                new { Count = count },
                commandType: CommandType.StoredProcedure
            )).ToList();
            return await FilterStoresBySiteAsync(conn, list);
        }

        public async Task<List<Store>> GetByUserIdAsync(int userId)
        {
            using var conn = _factory.CreateConnection();
            return (await conn.QueryAsync<Store>(
                "koch.spStores_GetByUserId",
                new { UserId = userId },
                commandType: CommandType.StoredProcedure
            )).ToList();
        }

        public async Task<Store> GetByIdAsync(int storeId)
        {
            using var conn = _factory.CreateConnection();
            var store = await conn.QueryFirstOrDefaultAsync<Store>(
                "koch.spStores_GetById",
                new { StoreId = storeId },
                commandType: CommandType.StoredProcedure);

            if (store == null || !SiteScope.ShouldFilter(_tenant))
                return store;

            try
            {
                var sid = await conn.ExecuteScalarAsync<int?>(
                    "SELECT SiteId FROM koch.Stores WHERE StoreID = @Id",
                    new { Id = storeId });
                store.SiteId = sid;
                if (sid.HasValue && sid.Value != _tenant.SiteId)
                    return null;
            }
            catch { /* ignore */ }

            return store;
        }

        public async Task<int> CreateAsync(Store store)
        {
            using var conn = _factory.CreateConnection();
            var parameters = new DynamicParameters();
            parameters.Add("UserId", store.UserID);
            parameters.Add("StoreName", store.StoreName);
            parameters.Add("StoreDescription", store.StoreDescription);
            parameters.Add("StoreAddress", store.StoreAddress);
            parameters.Add("StoreFamilyName", store.StoreFamilyName);
            parameters.Add("StoreCity", store.StoreCity);
            parameters.Add("StorePostCode", store.StorePostCode);
            parameters.Add("StorePhone", store.StorePhone);
            parameters.Add("StorePos1", store.StorePos1);
            parameters.Add("StorePos2", store.StorePos2);
            parameters.Add("StorePos3", store.StorePos3);
            parameters.Add("CompanyId", store.CompanyId);
            parameters.Add("CategoryId", store.CategoryId);
            parameters.Add("StoreId", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await conn.ExecuteAsync(
                "koch.spStores_Create",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            var newId = parameters.Get<int>("StoreId");
            var siteId = store.SiteId ?? (_tenant != null && _tenant.HasSite ? _tenant.SiteId : (int?)null);
            if (newId > 0 && siteId.HasValue && siteId.Value > 0)
            {
                try
                {
                    await conn.ExecuteAsync(
                        "UPDATE koch.Stores SET SiteId = @SiteId WHERE StoreID = @StoreId",
                        new { SiteId = siteId.Value, StoreId = newId });
                }
                catch { /* ستون هنوز migrate نشده */ }
            }

            return newId;
        }

        public async Task<bool> UpdateAsync(Store store)
        {
            using var conn = _factory.CreateConnection();
            var parameters = new DynamicParameters();
            parameters.Add("StoreId", store.StoreID);
            parameters.Add("StoreName", store.StoreName);
            parameters.Add("StoreDescription", store.StoreDescription);
            parameters.Add("StoreAddress", store.StoreAddress);
            parameters.Add("StoreFamilyName", store.StoreFamilyName);
            parameters.Add("StoreCity", store.StoreCity);
            parameters.Add("StorePostCode", store.StorePostCode);
            parameters.Add("StorePhone", store.StorePhone);
            parameters.Add("StorePos1", store.StorePos1);
            parameters.Add("StorePos2", store.StorePos2);
            parameters.Add("StorePos3", store.StorePos3);
            // فیلدهای جدید
            parameters.Add("StoreLatitude", store.StoreLatitude);
            parameters.Add("StoreLongitude", store.StoreLongitude);
            parameters.Add("StoreProfileImageUrl", store.StoreProfileImageUrl);
            parameters.Add("StoreOpeningHours", store.StoreOpeningHours);
            parameters.Add("StoreClosingHours", store.StoreClosingHours);
            parameters.Add("StoreWorkingDays", store.StoreWorkingDays);
            parameters.Add("StoreWebsite", store.StoreWebsite);
            parameters.Add("StoreSocialMedia", store.StoreSocialMedia);
            parameters.Add("IsLocationEnabled", store.IsLocationEnabled);
            parameters.Add("CompanyId", store.CompanyId);
            parameters.Add("CategoryId", store.CategoryId);
            parameters.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await conn.ExecuteAsync(
                "koch.spStores_Update",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return parameters.Get<int>("Result") == 1;
        }

        public async Task<(bool CanDelete, List<string> Reasons)> GetDeleteBlockersAsync(int storeId)
        {
            var reasons = new List<string>();
            using var conn = _factory.CreateConnection();

            async Task CountSafe(string sql, string label)
            {
                try
                {
                    var n = await conn.ExecuteScalarAsync<int>(sql, new { StoreId = storeId });
                    if (n > 0) reasons.Add($"{label}: {n}");
                }
                catch { /* table may not exist */ }
            }

            await CountSafe("SELECT COUNT(1) FROM koch.Orders WHERE StoreId=@StoreId", "سفارش");
            await CountSafe("SELECT COUNT(1) FROM koch.Copon WHERE StoreId=@StoreId", "کوپن");
            await CountSafe("SELECT COUNT(1) FROM koch.Products WHERE StoreId=@StoreId", "محصول");
            await CountSafe(@"SELECT COUNT(1) FROM koch.InventoryStock s
                INNER JOIN koch.Warehouses w ON w.Id = s.WarehouseId WHERE w.StoreId=@StoreId", "موجودی انبار");
            await CountSafe("SELECT COUNT(1) FROM koch.Warehouses WHERE StoreId=@StoreId", "انبار");

            return (reasons.Count == 0, reasons);
        }

        public async Task<bool> DeleteAsync(int storeId)
        {
            var (can, reasons) = await GetDeleteBlockersAsync(storeId);
            if (!can)
                throw new InvalidOperationException(
                    "امکان حذف نیست — با پشتیبانی صحبت کنید. وابستگی‌ها: " + string.Join("، ", reasons));

            using var conn = _factory.CreateConnection();
            var parameters = new DynamicParameters();
            parameters.Add("StoreId", storeId);
            parameters.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await conn.ExecuteAsync(
                "koch.spStores_Delete",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return parameters.Get<int>("Result") == 1;
        }

        public async Task<bool> ToggleStatusAsync(int storeId, bool status)
        {
            using var conn = _factory.CreateConnection();
            var parameters = new DynamicParameters();
            parameters.Add("StoreId", storeId);
            parameters.Add("StoreStat", status ? 1 : 0);
            parameters.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await conn.ExecuteAsync(
                "koch.spStores_ToggleStatus",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return parameters.Get<int>("Result") == 1;
        }

        public async Task<List<Store>> GetAllAsync()
        {
            return await GetAllScopedAsync(DataScopeQuery.Unrestricted());
        }

        public async Task<List<Store>> GetAllScopedAsync(DataScopeQuery scope)
        {
            using var conn = _factory.CreateConnection();
            var scopeKind = scope?.Kind ?? "All";
            var scopeValues = scope?.Values != null && scope.Values.Count > 0
                ? string.Join(",", scope.Values) : null;
            var sql = $@"
                SELECT s.*
                FROM koch.Stores s
                WHERE {DataScopeSqlBuilder.StoreScopePredicate("s")}
                ORDER BY s.StoreID DESC";
            var list = (await conn.QueryAsync<Store>(sql, new
            {
                ScopeKind = scopeKind,
                ScopeValues = scopeValues,
                ActorUserId = scope?.ActorUserId
            })).ToList();
            return await FilterStoresBySiteAsync(conn, list);
        }

        public async Task<int> CountScopedAsync(DataScopeQuery scope)
        {
            using var conn = _factory.CreateConnection();
            var scopeKind = scope?.Kind ?? "All";
            var scopeValues = scope?.Values != null && scope.Values.Count > 0
                ? string.Join(",", scope.Values) : null;
            var sql = $@"
                SELECT COUNT(1)
                FROM koch.Stores s
                WHERE {DataScopeSqlBuilder.StoreScopePredicate("s")}";
            var count = await conn.ExecuteScalarAsync<int>(sql, new
            {
                ScopeKind = scopeKind,
                ScopeValues = scopeValues,
                ActorUserId = scope?.ActorUserId
            });
            if (!SiteScope.ShouldFilter(_tenant))
                return count;
            try
            {
                var siteId = _tenant.SiteId;
                var siteCount = await conn.ExecuteScalarAsync<int>(
                    "SELECT COUNT(1) FROM koch.Stores WHERE SiteId IS NULL OR SiteId = @SiteId",
                    new { SiteId = siteId });
                return Math.Min(count, siteCount);
            }
            catch
            {
                return count;
            }
        }

        async Task<List<Store>> FilterStoresBySiteAsync(System.Data.IDbConnection conn, List<Store> list)
        {
            if (list == null || list.Count == 0 || !SiteScope.ShouldFilter(_tenant))
                return list ?? new List<Store>();

            try
            {
                var ids = list.Select(s => s.StoreID > 0 ? s.StoreID : s.Id).Distinct().Where(id => id > 0).ToList();
                if (ids.Count == 0) return list;

                var rows = (await conn.QueryAsync<(int StoreKey, int? SiteId)>(@"
                    SELECT StoreID AS StoreKey, SiteId
                    FROM koch.Stores
                    WHERE StoreID IN @Ids",
                    new { Ids = ids })).ToList();

                var map = new Dictionary<int, int?>();
                foreach (var r in rows)
                    map[r.StoreKey] = r.SiteId;

                var sid = _tenant.SiteId;
                return list.Where(s =>
                {
                    var key = s.StoreID > 0 ? s.StoreID : s.Id;
                    if (!map.TryGetValue(key, out var storeSite))
                        return true;
                    s.SiteId = storeSite;
                    return storeSite == null || storeSite == sid;
                }).ToList();
            }
            catch
            {
                return list;
            }
        }

    }
}
