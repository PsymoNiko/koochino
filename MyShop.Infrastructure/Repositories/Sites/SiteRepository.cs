using Dapper;
using MyShop.Application.Interfaces.Sites;
using MyShop.Core.Interfaces;
using MyShop.Core.Models.Sites;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Infrastructure.Repositories.Sites
{
    public class SiteRepository : ISiteRepository
    {
        private readonly IDbConnectionFactory _factory;

        public SiteRepository(IDbConnectionFactory factory)
        {
            _factory = factory;
        }

        public async Task<List<Site>> ListAsync(bool activeOnly = false)
        {
            using var conn = _factory.CreateConnection();
            var sql = activeOnly
                ? "SELECT Id, Code, Name, IsActive, BrandJson, CreatedAt, UpdatedAt FROM koch.Sites WHERE IsActive = 1 ORDER BY Id"
                : "SELECT Id, Code, Name, IsActive, BrandJson, CreatedAt, UpdatedAt FROM koch.Sites ORDER BY Id";
            var sites = (await conn.QueryAsync<Site>(sql)).ToList();
            if (sites.Count == 0) return sites;

            var domains = (await conn.QueryAsync<SiteDomain>(
                "SELECT Id, SiteId, Host, IsPrimary FROM koch.SiteDomains")).ToList();
            foreach (var s in sites)
                s.Domains = domains.Where(d => d.SiteId == s.Id).ToList();
            return sites;
        }

        public async Task<Site> GetByIdAsync(int id)
        {
            using var conn = _factory.CreateConnection();
            var site = await conn.QueryFirstOrDefaultAsync<Site>(
                "SELECT Id, Code, Name, IsActive, BrandJson, CreatedAt, UpdatedAt FROM koch.Sites WHERE Id = @Id",
                new { Id = id });
            if (site == null) return null;
            site.Domains = (await conn.QueryAsync<SiteDomain>(
                "SELECT Id, SiteId, Host, IsPrimary FROM koch.SiteDomains WHERE SiteId = @Id",
                new { Id = id })).ToList();
            return site;
        }

        public async Task<Site> GetByCodeAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return null;
            using var conn = _factory.CreateConnection();
            var site = await conn.QueryFirstOrDefaultAsync<Site>(
                "SELECT Id, Code, Name, IsActive, BrandJson, CreatedAt, UpdatedAt FROM koch.Sites WHERE Code = @Code",
                new { Code = code.Trim() });
            return site;
        }

        public async Task<ResolvedSite> ResolveByHostAsync(string host)
        {
            if (string.IsNullOrWhiteSpace(host)) return null;
            var h = host.Trim().ToLowerInvariant();
            if (h.StartsWith("www."))
                h = h.Substring(4);

            using var conn = _factory.CreateConnection();
            // exact host, then without port, then www variant
            var row = await conn.QueryFirstOrDefaultAsync<ResolvedSite>(@"
                SELECT TOP 1 s.Id AS SiteId, s.Code, s.Name, s.BrandJson, d.Host, s.IsActive
                FROM koch.SiteDomains d
                INNER JOIN koch.Sites s ON s.Id = d.SiteId
                WHERE s.IsActive = 1 AND (
                    LOWER(d.Host) = @Host
                    OR LOWER(d.Host) = @HostNoPort
                    OR LOWER(d.Host) = CONCAT('www.', @HostNoPort)
                )
                ORDER BY CASE WHEN LOWER(d.Host) = @Host THEN 0 WHEN d.IsPrimary = 1 THEN 1 ELSE 2 END",
                new
                {
                    Host = host.Trim().ToLowerInvariant(),
                    HostNoPort = h.Contains(':') ? h.Split(':')[0] : h
                });

            return row;
        }

        public async Task<int> UpsertAsync(Site site)
        {
            using var conn = _factory.CreateConnection();
            if (site.Id > 0)
            {
                await conn.ExecuteAsync(@"
                    UPDATE koch.Sites
                    SET Code = @Code, Name = @Name, IsActive = @IsActive, BrandJson = @BrandJson, UpdatedAt = SYSDATETIME()
                    WHERE Id = @Id", site);
                return site.Id;
            }

            return await conn.ExecuteScalarAsync<int>(@"
                INSERT INTO koch.Sites (Code, Name, IsActive, BrandJson)
                OUTPUT INSERTED.Id
                VALUES (@Code, @Name, @IsActive, @BrandJson)", site);
        }

        public async Task SetDomainsAsync(int siteId, IEnumerable<SiteDomain> domains)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync("DELETE FROM koch.SiteDomains WHERE SiteId = @SiteId", new { SiteId = siteId });
            foreach (var d in domains ?? Enumerable.Empty<SiteDomain>())
            {
                if (string.IsNullOrWhiteSpace(d.Host)) continue;
                await conn.ExecuteAsync(@"
                    INSERT INTO koch.SiteDomains (SiteId, Host, IsPrimary)
                    VALUES (@SiteId, @Host, @IsPrimary)",
                    new { SiteId = siteId, Host = d.Host.Trim().ToLowerInvariant(), d.IsPrimary });
            }
        }

        public async Task<List<int>> GetAdminSiteIdsAsync(int userId)
        {
            using var conn = _factory.CreateConnection();
            var ids = await conn.QueryAsync<int>(
                "SELECT SiteId FROM koch.AdminUserSites WHERE UserId = @UserId",
                new { UserId = userId });
            return ids?.ToList() ?? new List<int>();
        }

        public async Task SetAdminSitesAsync(int userId, IEnumerable<int> siteIds)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync("DELETE FROM koch.AdminUserSites WHERE UserId = @UserId", new { UserId = userId });
            foreach (var sid in (siteIds ?? Enumerable.Empty<int>()).Distinct())
            {
                if (sid <= 0) continue;
                await conn.ExecuteAsync(
                    "INSERT INTO koch.AdminUserSites (UserId, SiteId) VALUES (@UserId, @SiteId)",
                    new { UserId = userId, SiteId = sid });
            }
        }

        public async Task<bool> AdminHasSiteAccessAsync(int userId, int siteId)
        {
            if (siteId <= 0) return false;
            if (userId <= 0) return false;
            using var conn = _factory.CreateConnection();
            var countAssigned = await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(1) FROM koch.AdminUserSites WHERE UserId = @UserId",
                new { UserId = userId });
            // فاز ۹: بدون ردیف = دسترسی به هیچ سایتی (fail-closed). V009 باید backfill کرده باشد.
            if (countAssigned == 0) return false;
            var has = await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(1) FROM koch.AdminUserSites WHERE UserId = @UserId AND SiteId = @SiteId",
                new { UserId = userId, SiteId = siteId });
            return has > 0;
        }
    }
}
