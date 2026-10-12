using Dapper;
using MyShop.Application.Interfaces.Security;
using MyShop.Core.Interfaces;
using MyShop.Core.Models.Security;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Infrastructure.Repositories.Security
{
    public class PermissionRepository : IPermissionRepository
    {
        private readonly IDbConnectionFactory _factory;

        public PermissionRepository(IDbConnectionFactory factory)
        {
            _factory = factory;
        }

        public async Task<bool> IsEngineAvailableAsync()
        {
            using var conn = _factory.CreateConnection();
            var n = await conn.ExecuteScalarAsync<int?>(
                "SELECT OBJECT_ID(N'Security.spGetEffectivePermissions', N'P')");
            return n.HasValue && n.Value > 0;
        }

        public async Task<EngineCatalogStats> GetCatalogStatsAsync()
        {
            using var conn = _factory.CreateConnection();
            var versions = await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM Security.SchemaVersions");
            var modules = await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM Security.Modules");
            var perms = await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM Security.Permissions");
            return new EngineCatalogStats
            {
                SchemaVersionCount = versions,
                ModuleCount = modules,
                PermissionCount = perms
            };
        }

        public async Task<EffectivePermissions> GetEffectiveAsync(int userId, int? siteId = null)
        {
            using var conn = _factory.CreateConnection();
            using var multi = await conn.QueryMultipleAsync(
                "Security.spGetEffectivePermissions",
                new { UserId = userId, SiteId = siteId },
                commandType: CommandType.StoredProcedure);

            var meta = await multi.ReadFirstOrDefaultAsync<EffectivePermissionMeta>() ?? new EffectivePermissionMeta();
            var decisions = (await multi.ReadAsync<PermissionDecision>())?.ToList() ?? new List<PermissionDecision>();
            var scopes = (await multi.ReadAsync<PermissionScopeRow>())?.ToList() ?? new List<PermissionScopeRow>();

            return new EffectivePermissions
            {
                Meta = meta,
                Decisions = decisions,
                Scopes = scopes
            };
        }

        public async Task InvalidateUserStampAsync(int userId)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(
                "Security.spInvalidateUserSecurityStamp",
                new { UserId = userId },
                commandType: CommandType.StoredProcedure);
        }

        public async Task BumpGlobalVersionAsync()
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(
                "Security.spBumpGlobalSecurityVersion",
                commandType: CommandType.StoredProcedure);
        }

        public async Task InsertAuditAsync(AuthorizationAuditEntry entry)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(
                "Security.spAuthorizationAudit_Insert",
                new
                {
                    entry.ActorUserId,
                    entry.ActorRole,
                    entry.EventType,
                    entry.TargetType,
                    entry.TargetId,
                    entry.TargetUserId,
                    entry.SiteId,
                    entry.BeforeJson,
                    entry.AfterJson,
                    entry.ClientIp,
                    entry.UserAgent,
                    entry.CorrelationId,
                    entry.Success
                },
                commandType: CommandType.StoredProcedure);
        }

        public async Task<string> GetActivePolicyConditionJsonAsync(string permissionCode)
        {
            if (string.IsNullOrWhiteSpace(permissionCode)) return null;
            using var conn = _factory.CreateConnection();
            try
            {
                return await conn.ExecuteScalarAsync<string>(@"
                    SELECT TOP 1 pr.ConditionJson
                    FROM Security.PolicyRules pr
                    LEFT JOIN Security.Permissions p ON p.Id = pr.PermissionId
                    WHERE pr.IsActive = 1
                      AND (p.Code = @PermissionCode OR pr.Code = @PermissionCode)
                    ORDER BY pr.Priority",
                    new { PermissionCode = permissionCode });
            }
            catch
            {
                return null;
            }
        }
    }
}
