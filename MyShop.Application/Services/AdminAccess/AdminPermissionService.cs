using MyShop.Application.Interfaces.AdminAccess;
using MyShop.Application.Interfaces.Security;
using MyShop.Core.Models.AdminAccess;
using MyShop.Core.Models.Security;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Application.Services.AdminAccess
{
    public class AdminPermissionService : IAdminPermissionService
    {
        private readonly IAdminAccessRepository _repo;
        private readonly IPermissionService _permissionEngine;
        private readonly IConfiguration _config;
        private static readonly ConcurrentDictionary<int, (AdminEffectivePermissions Value, DateTime Expires)> Cache = new();
        private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

        public AdminPermissionService(
            IAdminAccessRepository repo,
            IPermissionService permissionEngine,
            IConfiguration config = null)
        {
            _repo = repo;
            _permissionEngine = permissionEngine;
            _config = config;
        }

        public async Task<AdminEffectivePermissions> GetEffectiveAsync(int userId)
        {
            if (userId <= 0)
                return new AdminEffectivePermissions { UserId = userId };

            if (await IsEngineOnSafeAsync())
            {
                AdminEffectivePermissions mapped = null;
                try
                {
                    mapped = await MapFromEngineAsync(userId);
                }
                catch
                {
                    mapped = null;
                }

                if (mapped != null && (mapped.IsFullAccess || mapped.Permissions?.Count > 0))
                    return mapped;

                try
                {
                    var legacy = await _repo.GetEffectiveAsync(userId);
                    if (legacy != null && (legacy.IsFullAccess || legacy.ProfileId > 0))
                        return legacy;
                }
                catch
                {
                    // legacy tables optional
                }

                return mapped ?? new AdminEffectivePermissions { UserId = userId };
            }

            if (Cache.TryGetValue(userId, out var entry) && entry.Expires > DateTime.UtcNow && entry.Value != null)
                return entry.Value;

            var eff = await _repo.GetEffectiveAsync(userId) ?? new AdminEffectivePermissions { UserId = userId };
            Cache[userId] = (eff, DateTime.UtcNow.Add(CacheTtl));
            return eff;
        }

        public async Task<bool> HasAsync(int userId, string moduleKey, string action)
        {
            if (userId <= 0 || string.IsNullOrWhiteSpace(moduleKey))
                return false;

            var normalizedAction = string.IsNullOrWhiteSpace(action)
                ? AdminPermissionActions.Read
                : action;

            if (await IsEngineOnSafeAsync())
            {
                if (await TryEnginePermissionAsync(userId, moduleKey, normalizedAction))
                    return true;

                if (_config?.GetValue("Authorization:LegacyAdminImpliesAll", false) == true
                    && await TryLegacyPermissionAsync(userId, moduleKey, AdminPermissionActions.Read, fullAccessOnly: true))
                {
                    return true;
                }

                return await TryLegacyPermissionAsync(userId, moduleKey, normalizedAction, fullAccessOnly: false);
            }

            var eff = await GetEffectiveAsync(userId);
            return eff.Has(moduleKey, normalizedAction);
        }

        async Task<bool> IsEngineOnSafeAsync()
        {
            try
            {
                return await _permissionEngine.IsEngineEnabledAsync();
            }
            catch
            {
                return false;
            }
        }

        async Task<bool> TryEnginePermissionAsync(int userId, string moduleKey, string action)
        {
            try
            {
                var code = AdminActionMap.ToPermissionCode(moduleKey, action);
                return await _permissionEngine.HasPermissionAsync(userId, code);
            }
            catch
            {
                return false;
            }
        }

        async Task<bool> TryLegacyPermissionAsync(int userId, string moduleKey, string action, bool fullAccessOnly)
        {
            try
            {
                var legacy = await GetLegacyEffectiveAsync(userId);
                if (legacy == null)
                    return false;
                if (legacy.IsFullAccess)
                    return true;
                if (fullAccessOnly)
                    return false;
                return legacy.Has(moduleKey, action);
            }
            catch
            {
                return false;
            }
        }

        public Task<List<AdminAccessProfile>> ListProfilesAsync() => _repo.ListProfilesAsync();

        public Task<AdminAccessProfile> GetProfileAsync(int id) => _repo.GetProfileAsync(id);

        public async Task<int> UpsertProfileAsync(AdminAccessProfile profile, IEnumerable<AdminModulePermission> permissions)
        {
            var id = await _repo.UpsertProfileAsync(profile);
            if (!profile.IsFullAccess)
                await _repo.ReplacePermissionsAsync(id, permissions);
            Cache.Clear();
            await _permissionEngine.InvalidateAllAsync();
            return id;
        }

        public async Task DeleteProfileAsync(int id)
        {
            await _repo.DeleteProfileAsync(id);
            Cache.Clear();
            await _permissionEngine.InvalidateAllAsync();
        }

        public async Task AssignUserProfileAsync(int userId, int profileId)
        {
            await _repo.AssignUserProfileAsync(userId, profileId);
            InvalidateCache(userId);
            await _permissionEngine.InvalidateUserAsync(userId);
        }

        public Task<UserAdminProfileAssignment> GetUserAssignmentAsync(int userId) =>
            _repo.GetUserAssignmentAsync(userId);

        public Task<List<AdminAccessOverview>> ListAdminOverviewAsync() =>
            _repo.ListAdminOverviewAsync();

        public void InvalidateCache(int userId)
        {
            Cache.TryRemove(userId, out _);
        }

        async Task<AdminEffectivePermissions> GetLegacyEffectiveAsync(int userId)
        {
            if (Cache.TryGetValue(userId, out var entry) && entry.Expires > DateTime.UtcNow && entry.Value != null)
                return entry.Value;

            var eff = await _repo.GetEffectiveAsync(userId);
            if (eff != null)
                Cache[userId] = (eff, DateTime.UtcNow.Add(CacheTtl));
            return eff;
        }

        async Task<AdminEffectivePermissions> MapFromEngineAsync(int userId)
        {
            UserAdminProfileAssignment legacyAssignment = null;
            try
            {
                legacyAssignment = await _repo.GetUserAssignmentAsync(userId);
            }
            catch
            {
                // legacy tables optional when engine is primary
            }

            EffectivePermissions eff;
            try
            {
                eff = await _permissionEngine.GetEffectiveAsync(userId);
            }
            catch
            {
                return null;
            }

            if (eff?.Decisions == null) return null;

            var perms = new Dictionary<string, AdminModulePermission>(StringComparer.OrdinalIgnoreCase);
            foreach (var d in eff.Decisions.Where(x => x.IsAllow))
            {
                var parts = (d.Code ?? "").Split('.', 2);
                if (parts.Length != 2) continue;
                var module = parts[0];
                if (!perms.TryGetValue(module, out var row))
                {
                    row = new AdminModulePermission { ModuleKey = module };
                    perms[module] = row;
                }
                switch (parts[1])
                {
                    case "View": row.CanRead = true; break;
                    case "Create": row.CanCreate = true; break;
                    case "Edit": row.CanUpdate = true; break;
                    case "Delete": row.CanDelete = true; break;
                }
            }

            var isFull = legacyAssignment?.IsFullAccess == true
                || perms.Count >= 20;

            return new AdminEffectivePermissions
            {
                UserId = userId,
                ProfileId = legacyAssignment?.ProfileId ?? (perms.Count > 0 ? 1 : 0),
                IsFullAccess = isFull,
                Permissions = perms.Values.ToList()
            };
        }
    }
}
