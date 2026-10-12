using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyShop.Application.Interfaces.Security;
using MyShop.Core.Models.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MyShop.Application.Services.Security
{
    public class PermissionService : IPermissionService
    {
        private readonly IPermissionRepository _repo;
        private readonly IDistributedCache _cache;
        private readonly IConfiguration _config;
        private readonly ILogger<PermissionService> _logger;
        private readonly IConditionEvaluator _conditionEvaluator;
        private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(15);

        public PermissionService(
            IPermissionRepository repo,
            [FromKeyedServices(PermissionCacheKeys.ServiceKey)] IDistributedCache cache,
            IConfiguration config,
            ILogger<PermissionService> logger,
            IConditionEvaluator conditionEvaluator = null)
        {
            _repo = repo;
            _cache = cache;
            _config = config;
            _logger = logger;
            _conditionEvaluator = conditionEvaluator ?? new DenyConditionalByDefaultEvaluator();
        }

        public Task<bool> IsEngineEnabledAsync()
        {
            if (!_config.GetValue("Authorization:UsePermissionEngine", false))
                return Task.FromResult(false);
            return _repo.IsEngineAvailableAsync();
        }

        public Task<bool> IsEngineAvailableAsync() => _repo.IsEngineAvailableAsync();

        public Task<EngineCatalogStats> GetCatalogStatsAsync() => _repo.GetCatalogStatsAsync();

        public async Task<EffectivePermissions> GetEffectiveAsync(int userId, int? siteId = null, CancellationToken ct = default)
        {
            if (userId <= 0) return new EffectivePermissions { Meta = new EffectivePermissionMeta { UserId = userId } };
            if (!await IsEngineEnabledAsync())
                return new EffectivePermissions { Meta = new EffectivePermissionMeta { UserId = userId, SiteId = siteId } };

            var eff = await _repo.GetEffectiveAsync(userId, siteId);
            eff.Meta.UserId = userId;
            eff.Meta.SiteId = siteId;
            return eff;
        }

        public async Task<bool> HasPermissionAsync(int userId, string permissionCode, int? siteId = null, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(permissionCode)) return false;

            if (!await IsEngineEnabledAsync())
            {
                // Deny by default (design A.2 rule #1) when the engine is off. This method
                // backs the generic [RequirePermission] policy used across arbitrary API
                // controllers (PermissionAuthorizationHandler) -- it has no way to know
                // whether a given permissionCode corresponds to something the legacy
                // Admin-role system would have granted, so it must never blanket-allow.
                // `Authorization:LegacyAdminImpliesAll` is an Admin-panel-specific bridge
                // (design R.3) and must only be applied inside AdminPermissionService's own
                // facade methods, which know they're specifically resolving Admin-module
                // permissions for a user already confirmed to hold the Admin role -- not
                // here, where it would silently grant every authenticated user every
                // permission on every [RequirePermission]-only controller regardless of role.
                return false;
            }

            var cacheKey = CacheKey(userId, siteId);
            var eff = await GetCachedOrLoadAsync(cacheKey, userId, siteId, ct);
            var ok = eff.Has(permissionCode);
            if (ok && _config.GetValue("Authorization:EnablePolicyRules", false))
            {
                var conditionalJson = await GetConditionalJsonAsync(permissionCode);
                if (!string.IsNullOrWhiteSpace(conditionalJson))
                {
                    var allowed = await _conditionEvaluator.EvaluateAsync(new PermissionEvaluationContext
                    {
                        UserId = userId,
                        PermissionCode = permissionCode,
                        SiteId = siteId,
                        ConditionJson = conditionalJson
                    }, ct);
                    if (!allowed)
                    {
                        _logger.LogDebug("Conditional permission denied (no evaluator): user={UserId} code={Code}", userId, permissionCode);
                        return false;
                    }
                }
            }
            if (!ok)
                _logger.LogDebug("Permission denied: user={UserId} code={Code}", userId, permissionCode);
            return ok;
        }

        public async Task<bool> HasAnyPermissionAsync(int userId, IEnumerable<string> codes, int? siteId = null, CancellationToken ct = default)
        {
            foreach (var c in codes)
                if (await HasPermissionAsync(userId, c, siteId, ct)) return true;
            return false;
        }

        public async Task<bool> HasAllPermissionsAsync(int userId, IEnumerable<string> codes, int? siteId = null, CancellationToken ct = default)
        {
            foreach (var c in codes)
                if (!await HasPermissionAsync(userId, c, siteId, ct)) return false;
            return true;
        }

        public async Task<ScopeDecision> GetScopeAsync(int userId, string permissionCode, int? siteId = null, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(permissionCode) || userId <= 0)
                return new ScopeDecision { Kind = "All", IsUnrestricted = true };

            if (!await IsEngineEnabledAsync())
                return new ScopeDecision { Kind = "All", IsUnrestricted = true };

            var cacheKey = CacheKey(userId, siteId);
            var eff = await GetCachedOrLoadAsync(cacheKey, userId, siteId, ct);
            var scopes = eff.Scopes?
                .Where(s => string.Equals(s.DataScopeCode, permissionCode, StringComparison.OrdinalIgnoreCase)
                    || MatchesPermissionScope(s, permissionCode, eff))
                .ToList() ?? new List<PermissionScopeRow>();

            // Match scopes by permission code on decisions
            var permId = eff.Decisions?.FirstOrDefault(d =>
                string.Equals(d.Code, permissionCode, StringComparison.OrdinalIgnoreCase))?.PermissionId;
            if (permId.HasValue)
            {
                scopes = eff.Scopes?
                    .Where(s => s.PermissionId == permId.Value)
                    .ToList() ?? new List<PermissionScopeRow>();
            }

            if (scopes.Count == 0)
                return new ScopeDecision { Kind = "All", IsUnrestricted = true };

            var ownScope = scopes.FirstOrDefault(s =>
                string.Equals(s.DataScopeCode, "Own", StringComparison.OrdinalIgnoreCase));
            if (ownScope != null)
            {
                return new ScopeDecision
                {
                    Kind = "Own",
                    IsUnrestricted = false,
                    Values = new[] { userId }
                };
            }

            var companyScope = scopes.FirstOrDefault(s =>
                string.Equals(s.DataScopeCode, "Company", StringComparison.OrdinalIgnoreCase));
            if (companyScope != null)
            {
                var vals = ParseScopeValues(companyScope.ScopeValue);
                return new ScopeDecision
                {
                    Kind = "Company",
                    IsUnrestricted = false,
                    Values = vals
                };
            }

            var siteScope = scopes.FirstOrDefault(s =>
                string.Equals(s.DataScopeCode, "Site", StringComparison.OrdinalIgnoreCase));
            if (siteScope != null && int.TryParse(siteScope.ScopeValue, out var siteVal))
            {
                return new ScopeDecision
                {
                    Kind = "Site",
                    IsUnrestricted = false,
                    Values = new[] { siteVal }
                };
            }

            return new ScopeDecision { Kind = "All", IsUnrestricted = true };
        }

        static IReadOnlyList<int> ParseScopeValues(string scopeValue)
        {
            if (string.IsNullOrWhiteSpace(scopeValue)) return Array.Empty<int>();
            var list = new List<int>();
            foreach (var part in scopeValue.Split(new[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries))
                if (int.TryParse(part.Trim(), out var n) && n > 0)
                    list.Add(n);
            return list;
        }

        static bool MatchesPermissionScope(PermissionScopeRow row, string permissionCode, EffectivePermissions eff)
        {
            if (row == null || eff?.Decisions == null) return false;
            var d = eff.Decisions.FirstOrDefault(x => x.PermissionId == row.PermissionId);
            return d != null && string.Equals(d.Code, permissionCode, StringComparison.OrdinalIgnoreCase);
        }

        public async Task InvalidateUserAsync(int userId, CancellationToken ct = default)
        {
            if (!await _repo.IsEngineAvailableAsync()) return;
            await _repo.InvalidateUserStampAsync(userId);
            try
            {
                await _cache.RemoveAsync(CacheKey(userId, null), ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Permission cache remove failed for user {UserId}", userId);
            }
        }

        public async Task InvalidateAllAsync(CancellationToken ct = default)
        {
            if (!await _repo.IsEngineAvailableAsync()) return;
            await _repo.BumpGlobalVersionAsync();
        }

        static string CacheKey(int userId, int? siteId) => $"perm:u{userId}:s{siteId ?? 0}";

        Task<string> GetConditionalJsonAsync(string permissionCode)
            => _repo.GetActivePolicyConditionJsonAsync(permissionCode);

        async Task<EffectivePermissions> GetCachedOrLoadAsync(string key, int userId, int? siteId, CancellationToken ct)
        {
            try
            {
                var bytes = await _cache.GetAsync(key, ct);
                if (bytes != null && bytes.Length > 0)
                    return JsonSerializer.Deserialize<EffectivePermissions>(bytes);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Permission cache read failed for {Key}", key);
            }

            var eff = await _repo.GetEffectiveAsync(userId, siteId);
            try
            {
                var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(eff));
                await _cache.SetAsync(key, bytes, new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = CacheTtl
                }, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Permission cache write failed for {Key}", key);
            }
            return eff;
        }
    }
}
