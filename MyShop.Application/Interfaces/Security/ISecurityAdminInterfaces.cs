using MyShop.Core.Models.Security;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MyShop.Application.Interfaces.Security
{
    public interface ISecurityAdminRepository
    {
        Task<List<SecurityRoleRow>> ListRolesAsync();
        Task<SecurityRoleRow> GetRoleAsync(int roleId);
        Task<int> UpsertRoleAsync(SecurityRoleRow role, int? actorUserId);
        Task DeleteRoleAsync(int roleId);
        Task<List<RolePermissionMatrixRow>> GetRoleMatrixAsync(int roleId);
        Task SetRolePermissionTriStateAsync(int roleId, int permissionId, byte triState, int? actorUserId);
        Task GrantRoleFullAccessAsync(int roleId, bool includeDescendants, int? actorUserId);
        Task<List<RoleHierarchyEdge>> ListHierarchyAsync();
        Task AddHierarchyAsync(int parentRoleId, int childRoleId, int? actorUserId);
        Task RemoveHierarchyAsync(int parentRoleId, int childRoleId);
        Task<List<UserPermissionOverrideRow>> ListUserOverridesAsync(int? userId = null);
        Task SetUserOverrideAsync(int userId, int permissionId, byte effect, string reason, System.DateTime expiresAt, int? actorUserId);
        Task RemoveUserOverrideAsync(int userId, int permissionId);
        Task<List<PermissionHolderRow>> WhoHasPermissionAsync(string permissionCode);
        Task<List<ActiveOverrideReportRow>> ListActiveOverridesAsync();
        Task<List<UnscopedGrantReportRow>> ListUnscopedGrantsAsync();
        Task<List<PermissionCatalogRow>> ListPermissionCatalogAsync();
        Task<List<PermissionTreeCatalogRow>> ListPermissionTreeCatalogAsync();
        Task<string> GetUsernameAsync(int userId);
        Task<List<DataScopeOptionRow>> ListDataScopesAsync();
        Task SetRolePermissionScopeAsync(int roleId, int permissionId, string scopeCode, string scopeValue, int? actorUserId);
    }

    public interface ISecurityAdminService
    {
        Task<List<SecurityRoleRow>> ListRolesAsync(CancellationToken ct = default);
        Task<SecurityRoleRow> GetRoleAsync(int roleId, CancellationToken ct = default);
        Task<int> SaveRoleAsync(SecurityRoleRow role, int? actorUserId, CancellationToken ct = default);
        Task DeleteRoleAsync(int roleId, int? actorUserId, CancellationToken ct = default);
        Task<List<RolePermissionMatrixRow>> GetRoleMatrixAsync(int roleId, CancellationToken ct = default);
        Task SetRolePermissionTriStateAsync(int roleId, int permissionId, byte triState, int? actorUserId, CancellationToken ct = default);
        Task GrantRoleFullAccessAsync(int roleId, bool includeDescendants, int? actorUserId, CancellationToken ct = default);
        Task<List<RoleHierarchyEdge>> ListHierarchyAsync(CancellationToken ct = default);
        Task AddHierarchyAsync(int parentRoleId, int childRoleId, int? actorUserId, CancellationToken ct = default);
        Task RemoveHierarchyAsync(int parentRoleId, int childRoleId, int? actorUserId, CancellationToken ct = default);
        Task<List<UserPermissionOverrideRow>> ListUserOverridesAsync(int? userId = null, CancellationToken ct = default);
        Task SetUserOverrideAsync(int userId, int permissionId, byte effect, string reason, System.DateTime expiresAt, int? actorUserId, CancellationToken ct = default);
        Task RemoveUserOverrideAsync(int userId, int permissionId, int? actorUserId, CancellationToken ct = default);
        Task<List<PermissionHolderRow>> WhoHasPermissionAsync(string permissionCode, CancellationToken ct = default);
        Task<List<ActiveOverrideReportRow>> ListActiveOverridesAsync(CancellationToken ct = default);
        Task<List<UnscopedGrantReportRow>> ListUnscopedGrantsAsync(CancellationToken ct = default);
        Task<List<PermissionCatalogRow>> ListPermissionCatalogAsync(CancellationToken ct = default);
        Task<UserPermissionTreeResult> GetUserPermissionTreeAsync(int userId, CancellationToken ct = default);
        Task<List<DataScopeOptionRow>> ListDataScopesAsync(CancellationToken ct = default);
        Task SetRolePermissionScopeAsync(int roleId, int permissionId, string scopeCode, string scopeValue, int? actorUserId, CancellationToken ct = default);
    }
}
