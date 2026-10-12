using MyShop.Core.Models.Security;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MyShop.Application.Interfaces.Security
{
    public interface IPermissionRepository
    {
        Task<bool> IsEngineAvailableAsync();
        Task<EngineCatalogStats> GetCatalogStatsAsync();
        Task<EffectivePermissions> GetEffectiveAsync(int userId, int? siteId = null);
        Task InvalidateUserStampAsync(int userId);
        Task BumpGlobalVersionAsync();
        Task InsertAuditAsync(AuthorizationAuditEntry entry);
        Task<string> GetActivePolicyConditionJsonAsync(string permissionCode);
    }

    public interface IPermissionService
    {
        Task<EffectivePermissions> GetEffectiveAsync(int userId, int? siteId = null, CancellationToken ct = default);
        Task<bool> HasPermissionAsync(int userId, string permissionCode, int? siteId = null, CancellationToken ct = default);
        Task<bool> HasAnyPermissionAsync(int userId, IEnumerable<string> codes, int? siteId = null, CancellationToken ct = default);
        Task<bool> HasAllPermissionsAsync(int userId, IEnumerable<string> codes, int? siteId = null, CancellationToken ct = default);
        Task<ScopeDecision> GetScopeAsync(int userId, string permissionCode, int? siteId = null, CancellationToken ct = default);
        Task InvalidateUserAsync(int userId, CancellationToken ct = default);
        Task InvalidateAllAsync(CancellationToken ct = default);
        Task<bool> IsEngineEnabledAsync();
        Task<bool> IsEngineAvailableAsync();
        Task<EngineCatalogStats> GetCatalogStatsAsync();
    }

    public sealed class EngineCatalogStats
    {
        public int SchemaVersionCount { get; set; }
        public int ModuleCount { get; set; }
        public int PermissionCount { get; set; }
    }

    public sealed class AuthorizationAuditEntry
    {
        public int? ActorUserId { get; set; }
        public string ActorRole { get; set; }
        public string EventType { get; set; }
        public string TargetType { get; set; }
        public int? TargetId { get; set; }
        public int? TargetUserId { get; set; }
        public int? SiteId { get; set; }
        public string BeforeJson { get; set; }
        public string AfterJson { get; set; }
        public string ClientIp { get; set; }
        public string UserAgent { get; set; }
        public System.Guid? CorrelationId { get; set; }
        public bool Success { get; set; } = true;
    }
}
