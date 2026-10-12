using MyShop.Application.Interfaces.Security;
using MyShop.Application.Security;
using MyShop.Core.Models.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MyShop.Application.Services.Security
{
    public class SecurityAdminService : ISecurityAdminService
    {
        private readonly ISecurityAdminRepository _repo;
        private readonly IPermissionRepository _permissionRepo;

        public SecurityAdminService(ISecurityAdminRepository repo, IPermissionRepository permissionRepo)
        {
            _repo = repo;
            _permissionRepo = permissionRepo;
        }

        public Task<List<SecurityRoleRow>> ListRolesAsync(CancellationToken ct = default) => _repo.ListRolesAsync();

        public Task<SecurityRoleRow> GetRoleAsync(int roleId, CancellationToken ct = default) => _repo.GetRoleAsync(roleId);

        public async Task<int> SaveRoleAsync(SecurityRoleRow role, int? actorUserId, CancellationToken ct = default)
        {
            var id = await _repo.UpsertRoleAsync(role, actorUserId);
            await AuditAsync(actorUserId, role.Id > 0 ? "Role.Update" : "Role.Create", "Role", id, null, role);
            await InvalidateAsync();
            return id;
        }

        public async Task DeleteRoleAsync(int roleId, int? actorUserId, CancellationToken ct = default)
        {
            await _repo.DeleteRoleAsync(roleId);
            await AuditAsync(actorUserId, "Role.Delete", "Role", roleId, null, null);
            await InvalidateAsync();
        }

        public async Task<List<RolePermissionMatrixRow>> GetRoleMatrixAsync(int roleId, CancellationToken ct = default)
        {
            var rows = await _repo.GetRoleMatrixAsync(roleId);
            foreach (var row in rows)
                SecurityCatalogLabels.Apply(row);
            return rows;
        }

        public async Task SetRolePermissionTriStateAsync(int roleId, int permissionId, byte triState, int? actorUserId, CancellationToken ct = default)
        {
            await _repo.SetRolePermissionTriStateAsync(roleId, permissionId, triState, actorUserId);
            await AuditAsync(actorUserId, "RolePermission.TriState", "RolePermission", roleId, permissionId,
                new { roleId, permissionId, triState });
            await InvalidateAsync();
        }

        public async Task GrantRoleFullAccessAsync(int roleId, bool includeDescendants, int? actorUserId, CancellationToken ct = default)
        {
            await _repo.GrantRoleFullAccessAsync(roleId, includeDescendants, actorUserId);
            await AuditAsync(actorUserId, "Role.FullAccess", "Role", roleId, null,
                new { includeDescendants });
            await InvalidateAsync();
        }

        public Task<List<RoleHierarchyEdge>> ListHierarchyAsync(CancellationToken ct = default) => _repo.ListHierarchyAsync();

        public async Task AddHierarchyAsync(int parentRoleId, int childRoleId, int? actorUserId, CancellationToken ct = default)
        {
            await _repo.AddHierarchyAsync(parentRoleId, childRoleId, actorUserId);
            await AuditAsync(actorUserId, "RoleHierarchy.Add", "RoleHierarchy", parentRoleId, childRoleId,
                new { parentRoleId, childRoleId });
            await InvalidateAsync();
        }

        public async Task RemoveHierarchyAsync(int parentRoleId, int childRoleId, int? actorUserId, CancellationToken ct = default)
        {
            await _repo.RemoveHierarchyAsync(parentRoleId, childRoleId);
            await AuditAsync(actorUserId, "RoleHierarchy.Remove", "RoleHierarchy", parentRoleId, childRoleId, null);
            await InvalidateAsync();
        }

        public Task<List<UserPermissionOverrideRow>> ListUserOverridesAsync(int? userId = null, CancellationToken ct = default)
            => _repo.ListUserOverridesAsync(userId);

        public async Task SetUserOverrideAsync(int userId, int permissionId, byte effect, string reason, DateTime expiresAt, int? actorUserId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("دلیل Override الزامی است.", nameof(reason));
            if (expiresAt <= DateTime.UtcNow)
                throw new ArgumentException("تاریخ انقضا باید در آینده باشد.", nameof(expiresAt));

            await _repo.SetUserOverrideAsync(userId, permissionId, effect, reason.Trim(), expiresAt, actorUserId);
            await AuditAsync(actorUserId, "UserOverride.Set", "UserPermission", userId, permissionId,
                new { effect, reason, expiresAt });
            await _permissionRepo.InvalidateUserStampAsync(userId);
            await InvalidateAsync();
        }

        public async Task RemoveUserOverrideAsync(int userId, int permissionId, int? actorUserId, CancellationToken ct = default)
        {
            await _repo.RemoveUserOverrideAsync(userId, permissionId);
            await AuditAsync(actorUserId, "UserOverride.Remove", "UserPermission", userId, permissionId, null);
            await _permissionRepo.InvalidateUserStampAsync(userId);
            await InvalidateAsync();
        }

        public Task<List<PermissionHolderRow>> WhoHasPermissionAsync(string permissionCode, CancellationToken ct = default)
            => _repo.WhoHasPermissionAsync(permissionCode);

        public Task<List<ActiveOverrideReportRow>> ListActiveOverridesAsync(CancellationToken ct = default)
            => _repo.ListActiveOverridesAsync();

        public Task<List<UnscopedGrantReportRow>> ListUnscopedGrantsAsync(CancellationToken ct = default)
            => _repo.ListUnscopedGrantsAsync();

        public async Task<List<PermissionCatalogRow>> ListPermissionCatalogAsync(CancellationToken ct = default)
        {
            var rows = await _repo.ListPermissionCatalogAsync();
            foreach (var row in rows)
                SecurityCatalogLabels.Apply(row);
            return rows;
        }

        public async Task<UserPermissionTreeResult> GetUserPermissionTreeAsync(int userId, CancellationToken ct = default)
        {
            if (userId <= 0)
                throw new ArgumentException("شناسه کاربر معتبر نیست.", nameof(userId));

            var catalog = await _repo.ListPermissionTreeCatalogAsync();
            var username = await _repo.GetUsernameAsync(userId);
            var effective = await _permissionRepo.GetEffectiveAsync(userId);
            var allowed = new HashSet<string>(
                effective?.Decisions?
                    .Where(d => d.IsAllow && !string.IsNullOrWhiteSpace(d.Code))
                    .Select(d => d.Code) ?? Enumerable.Empty<string>(),
                StringComparer.OrdinalIgnoreCase);

            return PermissionTreeBuilder.Build(userId, username, catalog, allowed);
        }

        public async Task<List<DataScopeOptionRow>> ListDataScopesAsync(CancellationToken ct = default)
        {
            var rows = await _repo.ListDataScopesAsync();
            foreach (var row in rows)
                SecurityCatalogLabels.Apply(row);
            return rows;
        }

        public async Task SetRolePermissionScopeAsync(int roleId, int permissionId, string scopeCode, string scopeValue, int? actorUserId, CancellationToken ct = default)
        {
            await _repo.SetRolePermissionScopeAsync(roleId, permissionId, scopeCode, scopeValue, actorUserId);
            await AuditAsync(actorUserId, "RolePermission.Scope", "RolePermission", roleId, permissionId,
                new { scopeCode, scopeValue });
            await InvalidateAsync();
        }

        async Task InvalidateAsync()
        {
            if (await _permissionRepo.IsEngineAvailableAsync())
                await _permissionRepo.BumpGlobalVersionAsync();
        }

        async Task AuditAsync(int? actorUserId, string eventType, string targetType, int? targetId, int? targetUserId, object after)
        {
            try
            {
                if (!await _permissionRepo.IsEngineAvailableAsync()) return;
                await _permissionRepo.InsertAuditAsync(new AuthorizationAuditEntry
                {
                    ActorUserId = actorUserId,
                    EventType = eventType,
                    TargetType = targetType,
                    TargetId = targetId,
                    TargetUserId = targetUserId,
                    AfterJson = after == null ? null : JsonSerializer.Serialize(after),
                    Success = true
                });
            }
            catch
            {
                // audit failure must not block admin operations
            }
        }
    }
}
