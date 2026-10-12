using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using MyShop.Application.Interfaces.Modules;
using MyShop.Core.Models.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Application.Services.Modules
{
    public class SiteModuleService : ISiteModuleService
    {
        private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);
        private readonly ISiteModuleRepository _repo;
        private readonly IMemoryCache _cache;
        private readonly IConfiguration _configuration;

        public SiteModuleService(ISiteModuleRepository repo, IMemoryCache cache, IConfiguration configuration)
        {
            _repo = repo;
            _cache = cache;
            _configuration = configuration;
        }

        public async Task<bool> IsEnabledAsync(int siteId, string moduleKey, bool globalFallback = true)
        {
            if (string.IsNullOrWhiteSpace(moduleKey)) return true;

            var self = await IsEnabledSelfAsync(siteId, moduleKey, globalFallback);
            if (!self) return false;

            foreach (var dep in await ResolveDependsOnAsync(moduleKey))
            {
                if (!await IsEnabledSelfAsync(siteId, dep, globalFallback))
                    return false;
            }
            return true;
        }

        async Task<bool> IsEnabledSelfAsync(int siteId, string moduleKey, bool globalFallback)
        {
            if (siteId > 0)
            {
                var flags = await GetSiteFlagsCachedAsync(siteId);
                if (flags.TryGetValue(moduleKey, out var siteOn))
                    return siteOn;
            }

            if (!globalFallback)
                return true;

            return _configuration.GetValue($"Modules:{moduleKey}", true);
        }

        async Task<IEnumerable<string>> ResolveDependsOnAsync(string moduleKey)
        {
            try
            {
                var catalog = await _repo.GetCatalogAsync(activeOnly: false);
                var item = catalog.FirstOrDefault(c =>
                    string.Equals(c.ModuleKey, moduleKey, StringComparison.OrdinalIgnoreCase));
                if (item != null && !string.IsNullOrWhiteSpace(item.DependsOnCsv))
                    return item.DependsOn;
            }
            catch { /* catalog column may be missing */ }

            if (ModuleBundleDefs.DefaultDependsOn.TryGetValue(moduleKey, out var fallback))
                return fallback;
            return Array.Empty<string>();
        }

        public async Task<IReadOnlyDictionary<string, bool>> GetEffectiveFlagsAsync(int siteId, IReadOnlyDictionary<string, bool> globalFallback)
        {
            var result = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            if (globalFallback != null)
            {
                foreach (var kv in globalFallback)
                    result[kv.Key] = kv.Value;
            }

            if (siteId <= 0)
                return await ApplyDependencyFlagsAsync(siteId, result);

            try
            {
                var siteFlags = await GetSiteFlagsCachedAsync(siteId);
                foreach (var kv in siteFlags)
                    result[kv.Key] = kv.Value;
            }
            catch { }

            return await ApplyDependencyFlagsAsync(siteId, result);
        }

        async Task<IReadOnlyDictionary<string, bool>> ApplyDependencyFlagsAsync(int siteId, Dictionary<string, bool> flags)
        {
            // Commerce (و هر باندل دیگر) فقط وقتی وابستگی‌ها روشن باشند true می‌ماند
            var keys = flags.Keys.ToList();
            foreach (var key in keys)
            {
                if (!flags[key]) continue;
                foreach (var dep in await ResolveDependsOnAsync(key))
                {
                    var depOn = flags.TryGetValue(dep, out var v)
                        ? v
                        : await IsEnabledSelfAsync(siteId, dep, true);
                    if (!depOn)
                    {
                        flags[key] = false;
                        break;
                    }
                }
            }
            return flags;
        }

        public async Task<IReadOnlyList<SiteModuleEntitlement>> ListForSiteAsync(int siteId)
        {
            if (siteId <= 0) return Array.Empty<SiteModuleEntitlement>();
            try
            {
                return await _repo.ListForSiteAsync(siteId);
            }
            catch
            {
                return Array.Empty<SiteModuleEntitlement>();
            }
        }

        public async Task SaveSiteFlagsAsync(int siteId, IReadOnlyDictionary<string, bool> values, string source = ModuleSources.Core)
        {
            if (siteId <= 0 || values == null) return;
            IReadOnlyList<SiteModuleEntitlement> existing;
            try { existing = await _repo.ListForSiteAsync(siteId); }
            catch { existing = Array.Empty<SiteModuleEntitlement>(); }

            var existingByKey = new Dictionary<string, SiteModuleEntitlement>(StringComparer.OrdinalIgnoreCase);
            foreach (var e in existing)
                existingByKey[e.ModuleKey] = e;

            // وقتی باندل روشن می‌شود، وابستگی‌ها را هم روشن کن
            var expanded = new Dictionary<string, bool>(values, StringComparer.OrdinalIgnoreCase);
            foreach (var kv in values.Where(x => x.Value))
            {
                foreach (var dep in await ResolveDependsOnAsync(kv.Key))
                    expanded[dep] = true;
            }

            foreach (var kv in expanded)
            {
                var src = source ?? ModuleSources.Core;
                DateTime? expires = null;
                if (existingByKey.TryGetValue(kv.Key, out var prev))
                {
                    if (string.Equals(prev.Source, ModuleSources.Licensed, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(prev.Source, ModuleSources.Trial, StringComparison.OrdinalIgnoreCase))
                    {
                        src = prev.Source;
                        expires = prev.ExpiresAt;
                    }
                }
                await _repo.UpsertAsync(siteId, kv.Key, kv.Value, src, expires);
            }
            InvalidateCache(siteId);
        }

        public void InvalidateCache(int siteId)
        {
            if (siteId > 0)
                _cache.Remove(CacheKey(siteId));
        }

        async Task<Dictionary<string, bool>> GetSiteFlagsCachedAsync(int siteId)
        {
            var key = CacheKey(siteId);
            if (_cache.TryGetValue(key, out Dictionary<string, bool> cached))
                return cached;

            var dict = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            var rows = await _repo.ListForSiteAsync(siteId);
            foreach (var row in rows)
            {
                if (string.IsNullOrWhiteSpace(row.ModuleKey)) continue;
                dict[row.ModuleKey] = row.IsEffectivelyEnabled;
            }

            _cache.Set(key, dict, CacheTtl);
            return dict;
        }

        static string CacheKey(int siteId) => "site:modules:" + siteId;
    }
}
