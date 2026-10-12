using Microsoft.Extensions.Caching.Memory;
using MyShop.Application.Interfaces.Sites;
using MyShop.Core.Models.Sites;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace MyShop.Application.Services.Sites
{
    public class SiteService : ISiteService
    {
        private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(10);
        private readonly ISiteRepository _repo;
        private readonly IMemoryCache _cache;

        public SiteService(ISiteRepository repo, IMemoryCache cache)
        {
            _repo = repo;
            _cache = cache;
        }

        public Task<List<Site>> ListAsync(bool activeOnly = false) => _repo.ListAsync(activeOnly);

        public Task<Site> GetByIdAsync(int id) => _repo.GetByIdAsync(id);

        public Task<Site> GetByCodeAsync(string code) => _repo.GetByCodeAsync(code);

        public async Task<ResolvedSite> ResolveByHostAsync(string host, string connectionStringHint = null)
        {
            if (string.IsNullOrWhiteSpace(host)) return null;
            var key = "site:host:" + host.Trim().ToLowerInvariant();
            if (_cache.TryGetValue(key, out ResolvedSite cached))
                return cached;

            try
            {
                var resolved = await _repo.ResolveByHostAsync(host);
                if (resolved != null)
                    _cache.Set(key, resolved, CacheTtl);
                return resolved;
            }
            catch
            {
                // جدول هنوز ساخته نشده — null
                return null;
            }
        }

        public async Task<int> UpsertAsync(Site site)
        {
            var id = await _repo.UpsertAsync(site);
            InvalidateCaches(site?.Id ?? id);
            return id;
        }

        public async Task SetDomainsAsync(int siteId, IEnumerable<SiteDomain> domains)
        {
            await _repo.SetDomainsAsync(siteId, domains);
            InvalidateCaches(siteId);
        }

        public Task<List<int>> GetAdminSiteIdsAsync(int userId) => _repo.GetAdminSiteIdsAsync(userId);

        public Task SetAdminSitesAsync(int userId, IEnumerable<int> siteIds) => _repo.SetAdminSitesAsync(userId, siteIds);

        public Task<bool> AdminCanAccessSiteAsync(int userId, int siteId) => _repo.AdminHasSiteAccessAsync(userId, siteId);

        public async Task<SiteBrandCopy> GetBrandAsync(int siteId)
        {
            if (siteId <= 0) return null;
            var key = BrandCacheKey(siteId);
            if (_cache.TryGetValue(key, out SiteBrandCopy cached))
                return cached;

            var site = await _repo.GetByIdAsync(siteId);
            if (site == null) return null;

            SiteBrandCopy brand = null;
            if (!string.IsNullOrWhiteSpace(site.BrandJson))
            {
                try
                {
                    brand = JsonSerializer.Deserialize<SiteBrandCopy>(site.BrandJson,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                catch { /* ignore */ }
            }

            brand ??= new SiteBrandCopy();
            if (string.IsNullOrWhiteSpace(brand.SiteName))
                brand.SiteName = site.Name;
            if (string.IsNullOrWhiteSpace(brand.PageTitle))
                brand.PageTitle = site.Name;

            _cache.Set(key, brand, CacheTtl);
            return brand;
        }

        static string BrandCacheKey(int siteId) => "site:brand:" + siteId;

        void InvalidateCaches(int siteId)
        {
            if (siteId > 0)
                _cache.Remove(BrandCacheKey(siteId));
            // host keys expire via TTL
        }
    }
}
