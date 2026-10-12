using Dapper;
using MyShop.Application.Interfaces.Security;
using MyShop.Core.Interfaces;
using MyShop.Core.Models.Security;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Infrastructure.Repositories.Security
{
    public class SecurityAdminRepository : ISecurityAdminRepository
    {
        private readonly IDbConnectionFactory _factory;

        public SecurityAdminRepository(IDbConnectionFactory factory) => _factory = factory;

        public async Task<List<SecurityRoleRow>> ListRolesAsync()
        {
            using var conn = _factory.CreateConnection();
            var sql = @"
                SELECT r.Id, r.RoleName, r.Code, r.Description, r.IsSystem, r.IsActive,
                       (SELECT COUNT(DISTINCT ur.UserId) FROM koch.UserRoles ur WHERE ur.RoleId = r.Id) AS UserCount,
                       (SELECT COUNT(1) FROM Security.RoleHierarchy h WHERE h.ParentRoleId = r.Id) AS ChildRoleCount
                FROM koch.Roles r
                ORDER BY r.IsSystem DESC, r.RoleName";
            return (await conn.QueryAsync<SecurityRoleRow>(sql)).ToList();
        }

        public async Task<SecurityRoleRow> GetRoleAsync(int roleId)
        {
            using var conn = _factory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<SecurityRoleRow>(@"
                SELECT r.Id, r.RoleName, r.Code, r.Description, r.IsSystem, r.IsActive,
                       (SELECT COUNT(DISTINCT ur.UserId) FROM koch.UserRoles ur WHERE ur.RoleId = r.Id) AS UserCount,
                       (SELECT COUNT(1) FROM Security.RoleHierarchy h WHERE h.ParentRoleId = r.Id) AS ChildRoleCount
                FROM koch.Roles r WHERE r.Id = @RoleId", new { RoleId = roleId });
        }

        public async Task<int> UpsertRoleAsync(SecurityRoleRow role, int? actorUserId)
        {
            using var conn = _factory.CreateConnection();
            if (role.Id > 0)
            {
                await conn.ExecuteAsync(@"
                    UPDATE koch.Roles SET RoleName=@RoleName, Code=@Code, Description=@Description,
                           IsActive=@IsActive, UpdatedAt=SYSDATETIME()
                    WHERE Id=@Id AND IsSystem=0", role);
                return role.Id;
            }

            return await conn.ExecuteScalarAsync<int>(@"
                INSERT INTO koch.Roles (RoleName, Code, Description, IsSystem, IsActive, CreatedAt)
                VALUES (@RoleName, @Code, @Description, 0, @IsActive, SYSDATETIME());
                SELECT CAST(SCOPE_IDENTITY() AS INT);",
                new { role.RoleName, role.Code, role.Description, role.IsActive });
        }

        public async Task DeleteRoleAsync(int roleId)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync("DELETE FROM koch.Roles WHERE Id=@RoleId AND IsSystem=0", new { RoleId = roleId });
        }

        public async Task<List<RolePermissionMatrixRow>> GetRoleMatrixAsync(int roleId)
        {
            using var conn = _factory.CreateConnection();
            var sql = @"
                SELECT p.Id AS PermissionId,
                       p.Code AS PermissionCode,
                       m.Code AS ModuleCode,
                       m.Name AS ModuleName,
                       res.Code AS ResourceCode,
                       res.Name AS ResourceName,
                       a.Code AS ActionCode,
                       a.Name AS ActionName,
                       CASE
                           WHEN rp.Id IS NULL THEN 0
                           WHEN rp.Effect = 1 THEN 1
                           ELSE 2
                       END AS TriState,
                       ISNULL(rp.IsMandatory, 0) AS IsMandatory,
                       rp.Id AS RolePermissionId,
                       ISNULL(ds.Code, N'All') AS ScopeCode,
                       ps.ScopeValue
                FROM Security.Permissions p
                INNER JOIN Security.Resources res ON res.Id = p.ResourceId
                INNER JOIN Security.Modules m ON m.Id = res.ModuleId
                INNER JOIN Security.Actions a ON a.Id = p.ActionId
                LEFT JOIN Security.RolePermissions rp ON rp.PermissionId = p.Id AND rp.RoleId = @RoleId
                LEFT JOIN Security.PermissionScopes ps ON ps.RolePermissionId = rp.Id
                LEFT JOIN Security.DataScopes ds ON ds.Id = ps.DataScopeId
                WHERE p.IsActive = 1 AND m.IsActive = 1 AND res.IsActive = 1
                ORDER BY m.SortOrder, m.Name, res.Depth, res.Name, a.SortOrder";
            return (await conn.QueryAsync<RolePermissionMatrixRow>(sql, new { RoleId = roleId })).ToList();
        }

        public async Task SetRolePermissionTriStateAsync(int roleId, int permissionId, byte triState, int? actorUserId)
        {
            using var conn = _factory.CreateConnection();
            if (triState == PermissionTriState.Inherited)
            {
                await conn.ExecuteAsync(
                    "DELETE FROM Security.RolePermissions WHERE RoleId=@RoleId AND PermissionId=@PermissionId",
                    new { RoleId = roleId, PermissionId = permissionId });
                return;
            }

            var effect = triState == PermissionTriState.Allow ? PermissionEffect.Allow : PermissionEffect.Deny;
            await conn.ExecuteAsync(@"
                IF EXISTS (SELECT 1 FROM Security.RolePermissions WHERE RoleId=@RoleId AND PermissionId=@PermissionId)
                    UPDATE Security.RolePermissions SET Effect=@Effect, UpdatedAt=SYSDATETIME(), CreatedByUserId=@ActorUserId
                    WHERE RoleId=@RoleId AND PermissionId=@PermissionId
                ELSE
                    INSERT INTO Security.RolePermissions (RoleId, PermissionId, Effect, CreatedByUserId)
                    VALUES (@RoleId, @PermissionId, @Effect, @ActorUserId)",
                new { RoleId = roleId, PermissionId = permissionId, Effect = effect, ActorUserId = actorUserId });
        }

        public async Task GrantRoleFullAccessAsync(int roleId, bool includeDescendants, int? actorUserId)
        {
            using var conn = _factory.CreateConnection();
            var moduleFilter = includeDescendants
                ? "1=1"
                : "res.ParentResourceId IS NULL";

            await conn.ExecuteAsync($@"
                MERGE Security.RolePermissions AS t
                USING (
                    SELECT @RoleId AS RoleId, p.Id AS PermissionId
                    FROM Security.Permissions p
                    INNER JOIN Security.Resources res ON res.Id = p.ResourceId
                    INNER JOIN Security.Modules m ON m.Id = res.ModuleId
                    WHERE p.IsActive = 1 AND m.IsActive = 1 AND res.IsActive = 1 AND {moduleFilter}
                ) AS s ON t.RoleId = s.RoleId AND t.PermissionId = s.PermissionId
                WHEN MATCHED THEN UPDATE SET Effect = 1, UpdatedAt = SYSDATETIME(), CreatedByUserId = @ActorUserId
                WHEN NOT MATCHED THEN INSERT (RoleId, PermissionId, Effect, CreatedByUserId)
                    VALUES (s.RoleId, s.PermissionId, 1, @ActorUserId);",
                new { RoleId = roleId, ActorUserId = actorUserId });
        }

        public async Task<List<RoleHierarchyEdge>> ListHierarchyAsync()
        {
            using var conn = _factory.CreateConnection();
            return (await conn.QueryAsync<RoleHierarchyEdge>(@"
                SELECT h.ParentRoleId, pr.RoleName AS ParentName, h.ChildRoleId, cr.RoleName AS ChildName
                FROM Security.RoleHierarchy h
                INNER JOIN koch.Roles pr ON pr.Id = h.ParentRoleId
                INNER JOIN koch.Roles cr ON cr.Id = h.ChildRoleId
                ORDER BY pr.RoleName, cr.RoleName")).ToList();
        }

        public async Task AddHierarchyAsync(int parentRoleId, int childRoleId, int? actorUserId)
        {
            if (parentRoleId == childRoleId)
                throw new InvalidOperationException("نقش نمی‌تواند والد خودش باشد.");

            using var conn = _factory.CreateConnection();
            var wouldCycle = await conn.ExecuteScalarAsync<int>(@"
                ;WITH Reach AS (
                    SELECT @ChildRoleId AS RoleId
                    UNION ALL
                    SELECT h.ChildRoleId
                    FROM Security.RoleHierarchy h
                    INNER JOIN Reach r ON h.ParentRoleId = r.RoleId
                    WHERE r.RoleId <> @ParentRoleId
                )
                SELECT CASE WHEN EXISTS (SELECT 1 FROM Reach WHERE RoleId = @ParentRoleId) THEN 1 ELSE 0 END",
                new { ParentRoleId = parentRoleId, ChildRoleId = childRoleId });

            if (wouldCycle > 0)
                throw new InvalidOperationException("این ارتباط باعث ایجاد حلقه در سلسله‌مراتب نقش‌ها می‌شود.");

            await conn.ExecuteAsync(@"
                IF NOT EXISTS (SELECT 1 FROM Security.RoleHierarchy WHERE ParentRoleId=@ParentRoleId AND ChildRoleId=@ChildRoleId)
                    INSERT INTO Security.RoleHierarchy (ParentRoleId, ChildRoleId, CreatedByUserId)
                    VALUES (@ParentRoleId, @ChildRoleId, @ActorUserId)",
                new { ParentRoleId = parentRoleId, ChildRoleId = childRoleId, ActorUserId = actorUserId });
        }

        public async Task RemoveHierarchyAsync(int parentRoleId, int childRoleId)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(
                "DELETE FROM Security.RoleHierarchy WHERE ParentRoleId=@ParentRoleId AND ChildRoleId=@ChildRoleId",
                new { ParentRoleId = parentRoleId, ChildRoleId = childRoleId });
        }

        public async Task<List<UserPermissionOverrideRow>> ListUserOverridesAsync(int? userId = null)
        {
            using var conn = _factory.CreateConnection();
            return (await conn.QueryAsync<UserPermissionOverrideRow>(@"
                SELECT up.Id, up.UserId, u.Username, up.PermissionId, p.Code AS PermissionCode,
                       up.Effect, up.Reason, up.ExpiresAt, up.CreatedAt
                FROM Security.UserPermissions up
                INNER JOIN koch.Users u ON u.Id = up.UserId
                INNER JOIN Security.Permissions p ON p.Id = up.PermissionId
                WHERE (@UserId IS NULL OR up.UserId = @UserId)
                ORDER BY up.CreatedAt DESC",
                new { UserId = userId })).ToList();
        }

        public async Task SetUserOverrideAsync(int userId, int permissionId, byte effect, string reason, DateTime expiresAt, int? actorUserId)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(@"
                IF EXISTS (SELECT 1 FROM Security.UserPermissions WHERE UserId=@UserId AND PermissionId=@PermissionId)
                    UPDATE Security.UserPermissions
                    SET Effect=@Effect, Reason=@Reason, ExpiresAt=@ExpiresAt, CreatedByUserId=@ActorUserId
                    WHERE UserId=@UserId AND PermissionId=@PermissionId
                ELSE
                    INSERT INTO Security.UserPermissions (UserId, PermissionId, Effect, Reason, ExpiresAt, CreatedByUserId)
                    VALUES (@UserId, @PermissionId, @Effect, @Reason, @ExpiresAt, @ActorUserId)",
                new { UserId = userId, PermissionId = permissionId, Effect = effect, Reason = reason, ExpiresAt = expiresAt, ActorUserId = actorUserId });
        }

        public async Task RemoveUserOverrideAsync(int userId, int permissionId)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(
                "DELETE FROM Security.UserPermissions WHERE UserId=@UserId AND PermissionId=@PermissionId",
                new { UserId = userId, PermissionId = permissionId });
        }

        public async Task<List<PermissionHolderRow>> WhoHasPermissionAsync(string permissionCode)
        {
            using var conn = _factory.CreateConnection();
            return (await conn.QueryAsync<PermissionHolderRow>(@"
                SELECT DISTINCT u.Id AS UserId, u.Username, N'Role' AS Source, r.RoleName
                FROM Security.Permissions p
                INNER JOIN Security.RolePermissions rp ON rp.PermissionId = p.Id AND rp.Effect = 1
                INNER JOIN koch.UserRoles ur ON ur.RoleId = rp.RoleId
                INNER JOIN koch.Users u ON u.Id = ur.UserId
                INNER JOIN koch.Roles r ON r.Id = rp.RoleId
                WHERE p.Code = @PermissionCode
                  AND (ur.ExpiresAt IS NULL OR ur.ExpiresAt > SYSDATETIME())
                UNION
                SELECT u.Id, u.Username, N'Override', NULL
                FROM Security.UserPermissions up
                INNER JOIN Security.Permissions p ON p.Id = up.PermissionId
                INNER JOIN koch.Users u ON u.Id = up.UserId
                WHERE p.Code = @PermissionCode AND up.Effect = 1
                  AND (up.ExpiresAt IS NULL OR up.ExpiresAt > SYSDATETIME())",
                new { PermissionCode = permissionCode })).ToList();
        }

        public async Task<List<ActiveOverrideReportRow>> ListActiveOverridesAsync()
        {
            using var conn = _factory.CreateConnection();
            return (await conn.QueryAsync<ActiveOverrideReportRow>(@"
                SELECT up.Id, up.UserId, u.Username, p.Code AS PermissionCode, up.Effect, up.Reason, up.ExpiresAt
                FROM Security.UserPermissions up
                INNER JOIN koch.Users u ON u.Id = up.UserId
                INNER JOIN Security.Permissions p ON p.Id = up.PermissionId
                WHERE up.ExpiresAt IS NULL OR up.ExpiresAt > SYSDATETIME()
                ORDER BY up.ExpiresAt, u.Username")).ToList();
        }

        public async Task<List<UnscopedGrantReportRow>> ListUnscopedGrantsAsync()
        {
            using var conn = _factory.CreateConnection();
            return (await conn.QueryAsync<UnscopedGrantReportRow>(@"
                SELECT rp.Id AS RolePermissionId, r.RoleName, p.Code AS PermissionCode
                FROM Security.RolePermissions rp
                INNER JOIN koch.Roles r ON r.Id = rp.RoleId
                INNER JOIN Security.Permissions p ON p.Id = rp.PermissionId
                WHERE rp.Effect = 1
                  AND NOT EXISTS (
                      SELECT 1 FROM Security.PermissionScopes ps WHERE ps.RolePermissionId = rp.Id
                  )
                  AND NOT EXISTS (
                      SELECT 1 FROM Security.UserPermissions up WHERE up.PermissionId = rp.PermissionId
                  )
                ORDER BY r.RoleName, p.Code")).ToList();
        }

        public async Task<List<PermissionCatalogRow>> ListPermissionCatalogAsync()
        {
            using var conn = _factory.CreateConnection();
            return (await conn.QueryAsync<PermissionCatalogRow>(@"
                SELECT p.Id, p.Code, m.Code AS ModuleCode, a.Code AS ActionCode,
                       m.Name AS ModuleName, a.Name AS ActionName
                FROM Security.Permissions p
                INNER JOIN Security.Resources res ON res.Id = p.ResourceId
                INNER JOIN Security.Modules m ON m.Id = res.ModuleId
                INNER JOIN Security.Actions a ON a.Id = p.ActionId
                WHERE p.IsActive = 1 AND m.IsActive = 1
                ORDER BY m.Name, a.SortOrder, p.Code")).ToList();
        }

        public async Task<List<PermissionTreeCatalogRow>> ListPermissionTreeCatalogAsync()
        {
            using var conn = _factory.CreateConnection();
            return (await conn.QueryAsync<PermissionTreeCatalogRow>(@"
                SELECT m.Id AS ModuleId, m.Code AS ModuleCode, m.Name AS ModuleName, m.SortOrder AS ModuleSort,
                       res.Id AS ResourceId, res.Code AS ResourceCode, res.Name AS ResourceName,
                       res.ParentResourceId, res.Depth AS ResourceDepth,
                       p.Id AS PermissionId, p.Code AS PermissionCode,
                       a.Code AS ActionCode, a.Name AS ActionName, a.SortOrder AS ActionSort
                FROM Security.Permissions p
                INNER JOIN Security.Resources res ON res.Id = p.ResourceId
                INNER JOIN Security.Modules m ON m.Id = res.ModuleId
                INNER JOIN Security.Actions a ON a.Id = p.ActionId
                WHERE p.IsActive = 1 AND m.IsActive = 1 AND res.IsActive = 1
                ORDER BY m.SortOrder, m.Name, res.Depth, res.Name, a.SortOrder, p.Code")).ToList();
        }

        public async Task<string> GetUsernameAsync(int userId)
        {
            if (userId <= 0) return null;
            using var conn = _factory.CreateConnection();
            return await conn.ExecuteScalarAsync<string>(
                "SELECT Username FROM koch.Users WHERE Id = @UserId",
                new { UserId = userId });
        }

        public async Task<List<DataScopeOptionRow>> ListDataScopesAsync()
        {
            using var conn = _factory.CreateConnection();
            return (await conn.QueryAsync<DataScopeOptionRow>(@"
                SELECT Id, Code, Name, RequiresValue
                FROM Security.DataScopes
                WHERE IsEnabled = 1
                ORDER BY SortOrder")).ToList();
        }

        public async Task SetRolePermissionScopeAsync(int roleId, int permissionId, string scopeCode, string scopeValue, int? actorUserId)
        {
            using var conn = _factory.CreateConnection();
            var rolePermId = await conn.ExecuteScalarAsync<int?>(@"
                SELECT Id FROM Security.RolePermissions
                WHERE RoleId = @RoleId AND PermissionId = @PermissionId AND Effect = 1",
                new { RoleId = roleId, PermissionId = permissionId });

            if (!rolePermId.HasValue || rolePermId.Value <= 0)
                throw new InvalidOperationException("ابتدا مجوز را روی «مجاز» بگذارید، سپس دامنه را تنظیم کنید.");

            await conn.ExecuteAsync(
                "DELETE FROM Security.PermissionScopes WHERE RolePermissionId = @RolePermissionId",
                new { RolePermissionId = rolePermId.Value });

            if (string.IsNullOrWhiteSpace(scopeCode) || string.Equals(scopeCode, "All", StringComparison.OrdinalIgnoreCase))
                return;

            var dataScopeId = await conn.ExecuteScalarAsync<int?>(
                "SELECT Id FROM Security.DataScopes WHERE Code = @Code AND IsEnabled = 1",
                new { Code = scopeCode });
            if (!dataScopeId.HasValue)
                throw new InvalidOperationException($"دامنه '{scopeCode}' در کاتالوگ یافت نشد.");

            await conn.ExecuteAsync(@"
                INSERT INTO Security.PermissionScopes (RolePermissionId, DataScopeId, ScopeValue)
                VALUES (@RolePermissionId, @DataScopeId, @ScopeValue)",
                new { RolePermissionId = rolePermId.Value, DataScopeId = dataScopeId.Value, ScopeValue = scopeValue });
        }
    }
}
