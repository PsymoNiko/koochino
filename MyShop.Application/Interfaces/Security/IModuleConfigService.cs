using System.Collections.Generic;
using System.Threading.Tasks;
using MyShop.Core.Models.Security;

namespace MyShop.Application.Interfaces.Security
{
    public interface IEncryptionService
    {
        int CurrentKeyVersion { get; }
        bool HasMasterKey { get; }
        byte[] Encrypt(string plainText, out int keyVersion);
        string Decrypt(byte[] blob, int keyVersion);
    }

    public interface IModuleSettingRepository
    {
        Task<ModuleSettingDefinition> GetDefinitionAsync(string moduleKey, string settingKey);
        Task<ModuleSettingValueRow> GetValueAsync(int? siteId, string moduleKey, string settingKey);
        Task<bool> GetAllowPlatformSecretFallbackAsync(int siteId);
        Task UpsertPlainAsync(int? siteId, string moduleKey, string settingKey, string plainValue, int? actorUserId);
        Task UpsertSecretAsync(int? siteId, string moduleKey, string settingKey, byte[] encrypted, int keyVersion, int? actorUserId);
        Task WriteAuditAsync(SecretAuditEntry entry);
        Task<IReadOnlyList<ModuleSettingDefinition>> ListDefinitionsAsync();
        Task EnsureDefinitionAsync(ModuleSettingDefinition definition);
        Task<bool> HasSecretStoredAsync(int? siteId, string moduleKey, string settingKey);
        Task<bool> GetWebhookEnforceAsync(int siteId);
        Task SetWebhookEnforceAsync(int siteId, bool enforce);
    }

    public interface IModuleConfigService
    {
        Task<string> GetStringAsync(int? siteId, string moduleKey, string settingKey, bool allowLegacyFallback = true);
        Task<string> GetSecretAsync(int? siteId, string moduleKey, string settingKey, bool allowLegacyFallback = true);
        Task SetStringAsync(int? siteId, string moduleKey, string settingKey, string value);
        Task SetSecretAsync(int? siteId, string moduleKey, string settingKey, string secret);
    }

    public interface IRequestAuditContext
    {
        int? UserId { get; }
        bool IsAdmin { get; }
        string ActorRole { get; }
        int? TenantSiteId { get; }
        string ClientIp { get; }
        string UserAgent { get; }
    }
}
