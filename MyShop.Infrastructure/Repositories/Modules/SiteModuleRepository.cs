using Dapper;
using MyShop.Application.Interfaces.Modules;
using MyShop.Core.Interfaces;
using MyShop.Core.Models.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Infrastructure.Repositories.Modules
{
    public class SiteModuleRepository : ISiteModuleRepository
    {
        private readonly IDbConnectionFactory _factory;

        public SiteModuleRepository(IDbConnectionFactory factory)
        {
            _factory = factory;
        }

        public async Task<IReadOnlyList<ModuleCatalogItem>> GetCatalogAsync(bool activeOnly = true, bool sellableOnly = false)
        {
            using var conn = _factory.CreateConnection();
            var sql = @"
                SELECT ModuleKey, DisplayName, Description, IsSellable, BasePrice, SortOrder, IsActive,
                       DependsOnCsv, IsBundle
                FROM koch.ModuleCatalog
                WHERE (@ActiveOnly = 0 OR IsActive = 1)
                  AND (@SellableOnly = 0 OR IsSellable = 1)
                ORDER BY SortOrder, ModuleKey";
            var rows = await conn.QueryAsync<ModuleCatalogItem>(sql, new
            {
                ActiveOnly = activeOnly ? 1 : 0,
                SellableOnly = sellableOnly ? 1 : 0
            });
            return rows.ToList();
        }

        public async Task<IReadOnlyList<SiteModuleEntitlement>> ListForSiteAsync(int siteId)
        {
            if (siteId <= 0) return Array.Empty<SiteModuleEntitlement>();
            using var conn = _factory.CreateConnection();
            var rows = await conn.QueryAsync<SiteModuleEntitlement>(@"
                SELECT sm.SiteId, sm.ModuleKey, sm.IsEnabled, sm.Source, sm.ExpiresAt, sm.UpdatedAt,
                       c.DisplayName, c.IsSellable
                FROM koch.SiteModules sm
                LEFT JOIN koch.ModuleCatalog c ON c.ModuleKey = sm.ModuleKey
                WHERE sm.SiteId = @SiteId
                ORDER BY ISNULL(c.SortOrder, 999), sm.ModuleKey",
                new { SiteId = siteId });
            return rows.ToList();
        }

        public async Task<SiteModuleEntitlement> GetAsync(int siteId, string moduleKey)
        {
            if (siteId <= 0 || string.IsNullOrWhiteSpace(moduleKey)) return null;
            using var conn = _factory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<SiteModuleEntitlement>(@"
                SELECT sm.SiteId, sm.ModuleKey, sm.IsEnabled, sm.Source, sm.ExpiresAt, sm.UpdatedAt,
                       c.DisplayName, c.IsSellable
                FROM koch.SiteModules sm
                LEFT JOIN koch.ModuleCatalog c ON c.ModuleKey = sm.ModuleKey
                WHERE sm.SiteId = @SiteId AND sm.ModuleKey = @ModuleKey",
                new { SiteId = siteId, ModuleKey = moduleKey.Trim() });
        }

        public async Task UpsertAsync(int siteId, string moduleKey, bool isEnabled, string source, DateTime? expiresAt)
        {
            if (siteId <= 0 || string.IsNullOrWhiteSpace(moduleKey)) return;
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(@"
                MERGE koch.SiteModules AS t
                USING (SELECT @SiteId AS SiteId, @ModuleKey AS ModuleKey) AS s
                ON t.SiteId = s.SiteId AND t.ModuleKey = s.ModuleKey
                WHEN MATCHED THEN
                    UPDATE SET IsEnabled = @IsEnabled, Source = @Source, ExpiresAt = @ExpiresAt, UpdatedAt = SYSDATETIME()
                WHEN NOT MATCHED THEN
                    INSERT (SiteId, ModuleKey, IsEnabled, Source, ExpiresAt)
                    VALUES (@SiteId, @ModuleKey, @IsEnabled, @Source, @ExpiresAt);",
                new
                {
                    SiteId = siteId,
                    ModuleKey = moduleKey.Trim(),
                    IsEnabled = isEnabled,
                    Source = string.IsNullOrWhiteSpace(source) ? ModuleSources.Core : source,
                    ExpiresAt = expiresAt
                });
        }

        public async Task UpsertManyAsync(int siteId, IEnumerable<(string ModuleKey, bool IsEnabled, string Source)> items)
        {
            if (siteId <= 0 || items == null) return;
            foreach (var item in items)
                await UpsertAsync(siteId, item.ModuleKey, item.IsEnabled, item.Source, null);
        }

        public Task SetEnabledAsync(int siteId, string moduleKey, bool isEnabled)
        {
            return UpsertAsync(siteId, moduleKey, isEnabled, ModuleSources.Core, null);
        }

        public async Task DeleteAsync(int siteId, string moduleKey)
        {
            if (siteId <= 0 || string.IsNullOrWhiteSpace(moduleKey)) return;
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(
                "DELETE FROM koch.SiteModules WHERE SiteId = @SiteId AND ModuleKey = @ModuleKey",
                new { SiteId = siteId, ModuleKey = moduleKey.Trim() });
        }
    }
}
