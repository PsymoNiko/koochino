using System;

namespace MyShop.Core.Models.Security
{
    public static class SettingFallbackPolicies
    {
        public const string None = "None";
        public const string PlatformIfAllowed = "PlatformIfAllowed";
        public const string LegacyConfigUntilMigrated = "LegacyConfigUntilMigrated";
    }

    public class ModuleSettingDefinition
    {
        public string ModuleKey { get; set; }
        public string SettingKey { get; set; }
        public string DisplayName { get; set; }
        public string GroupName { get; set; }
        public string ValueKind { get; set; }
        public bool IsSecret { get; set; }
        public string FallbackPolicy { get; set; }
        public string LegacyConfigPath { get; set; }
        public bool AllowEmpty { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; }
    }

    public class ModuleSettingValueRow
    {
        public int Id { get; set; }
        public int? SiteId { get; set; }
        public string ModuleKey { get; set; }
        public string SettingKey { get; set; }
        public bool IsSecret { get; set; }
        public string PlainValue { get; set; }
        public byte[] EncryptedValue { get; set; }
        public int? KeyVersion { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int? UpdatedByUserId { get; set; }
    }

    public class SecretAuditEntry
    {
        public string Action { get; set; }
        public string ModuleKey { get; set; }
        public string SettingKey { get; set; }
        public int? SiteId { get; set; }
        public int? ActorUserId { get; set; }
        public string ActorRole { get; set; }
        public string ClientIp { get; set; }
        public string UserAgent { get; set; }
        public int? KeyVersion { get; set; }
        public bool Success { get; set; }
    }
}
