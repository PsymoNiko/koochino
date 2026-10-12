using System;
using System.Collections.Generic;

namespace MyShop.Core.Models.Security
{
    public static class PermissionEffect
    {
        public const byte Deny = 0;
        public const byte Allow = 1;
    }

    /// <summary>
    /// DI keyed-service key for the permission engine's dedicated IDistributedCache
    /// instance (SQL Server-backed, see sql/security/V010). Kept deliberately separate
    /// from the app's default IDistributedCache (used by Session) so that a host can be
    /// restarted with this code before V010 has been run against the database without
    /// breaking Session -- the permission cache is only ever touched when
    /// Authorization:UsePermissionEngine is on, which requires V001-V010 to already
    /// be applied.
    /// </summary>
    public static class PermissionCacheKeys
    {
        public const string ServiceKey = "PermissionEngine";
    }

    public static class PermissionSources
    {
        public const string Role = "Role";
        public const string UserOverride = "UserOverride";
        public const string MandatoryDeny = "MandatoryDeny";
    }

    public sealed class EffectivePermissionMeta
    {
        public long GlobalVersion { get; set; }
        public string SecurityStamp { get; set; }
        public int UserId { get; set; }
        public int? SiteId { get; set; }
    }

    public sealed class PermissionDecision
    {
        public int PermissionId { get; set; }
        public string Code { get; set; }
        public byte Effect { get; set; }
        public string Source { get; set; }
        public bool IsAllow => Effect == PermissionEffect.Allow;
    }

    public sealed class PermissionScopeRow
    {
        public int PermissionId { get; set; }
        public string DataScopeCode { get; set; }
        public string ScopeValue { get; set; }
    }

    public sealed class EffectivePermissions
    {
        public EffectivePermissionMeta Meta { get; set; } = new();
        public List<PermissionDecision> Decisions { get; set; } = new();
        public List<PermissionScopeRow> Scopes { get; set; } = new();

        private HashSet<string> _allowCodes;

        public bool Has(string permissionCode)
        {
            if (string.IsNullOrWhiteSpace(permissionCode)) return false;
            _allowCodes ??= BuildAllowSet();
            return _allowCodes.Contains(permissionCode);
        }

        public bool HasAny(IEnumerable<string> codes)
        {
            foreach (var c in codes)
                if (Has(c)) return true;
            return false;
        }

        public bool HasAll(IEnumerable<string> codes)
        {
            foreach (var c in codes)
                if (!Has(c)) return false;
            return true;
        }

        HashSet<string> BuildAllowSet()
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var d in Decisions)
                if (d.IsAllow && !string.IsNullOrWhiteSpace(d.Code))
                    set.Add(d.Code);
            return set;
        }
    }

    public sealed class ScopeDecision
    {
        public string Kind { get; set; } = "All";
        public IReadOnlyList<int> Values { get; set; } = Array.Empty<int>();
        public bool IsUnrestricted { get; set; } = true;
    }

    public static class AdminActionMap
    {
        public static string ToPermissionAction(string adminAction) => adminAction switch
        {
            "Read" => "View",
            "Create" => "Create",
            "Update" => "Edit",
            "Delete" => "Delete",
            _ => adminAction
        };

        public static string ToPermissionCode(string moduleKey, string adminAction)
            => $"{moduleKey}.{ToPermissionAction(adminAction)}";
    }
}
