using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using MyShop.Application.Interfaces.Security;
using MyShop.Core.Interfaces;
using MyShop.Core.Models.Security;

namespace MyShop.Infrastructure.Repositories.Security
{
    public class ModuleSettingRepository : IModuleSettingRepository
    {
        readonly IDbConnectionFactory _factory;

        public ModuleSettingRepository(IDbConnectionFactory factory) => _factory = factory;

        public async Task<ModuleSettingDefinition> GetDefinitionAsync(string moduleKey, string settingKey)
        {
            using var conn = _factory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<ModuleSettingDefinition>(
                @"SELECT ModuleKey, SettingKey, DisplayName, GroupName, ValueKind, IsSecret, FallbackPolicy,
                         LegacyConfigPath, AllowEmpty, SortOrder, IsActive
                  FROM koch.ModuleSettingDefinitions
                  WHERE ModuleKey=@ModuleKey AND SettingKey=@SettingKey AND IsActive=1",
                new { ModuleKey = moduleKey, SettingKey = settingKey });
        }

        public async Task<ModuleSettingValueRow> GetValueAsync(int? siteId, string moduleKey, string settingKey)
        {
            using var conn = _factory.CreateConnection();
            if (siteId.HasValue && siteId.Value > 0)
            {
                return await conn.QueryFirstOrDefaultAsync<ModuleSettingValueRow>(
                    @"SELECT Id, SiteId, ModuleKey, SettingKey, IsSecret, PlainValue, EncryptedValue, KeyVersion, UpdatedAt, UpdatedByUserId
                      FROM koch.ModuleSettingValues
                      WHERE SiteId=@SiteId AND ModuleKey=@ModuleKey AND SettingKey=@SettingKey",
                    new { SiteId = siteId.Value, ModuleKey = moduleKey, SettingKey = settingKey });
            }

            return await conn.QueryFirstOrDefaultAsync<ModuleSettingValueRow>(
                @"SELECT Id, SiteId, ModuleKey, SettingKey, IsSecret, PlainValue, EncryptedValue, KeyVersion, UpdatedAt, UpdatedByUserId
                  FROM koch.ModuleSettingValues
                  WHERE SiteId IS NULL AND ModuleKey=@ModuleKey AND SettingKey=@SettingKey",
                new { ModuleKey = moduleKey, SettingKey = settingKey });
        }

        public async Task<bool> GetAllowPlatformSecretFallbackAsync(int siteId)
        {
            using var conn = _factory.CreateConnection();
            var v = await conn.ExecuteScalarAsync<bool?>(
                "SELECT AllowPlatformSecretFallback FROM koch.SiteSecurityFlags WHERE SiteId=@SiteId",
                new { SiteId = siteId });
            return v == true;
        }

        public async Task UpsertPlainAsync(int? siteId, string moduleKey, string settingKey, string plainValue, int? actorUserId)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(@"
MERGE koch.ModuleSettingValues AS t
USING (SELECT @SiteId AS SiteId, @ModuleKey AS ModuleKey, @SettingKey AS SettingKey) AS s
ON ((t.SiteId = s.SiteId) OR (t.SiteId IS NULL AND s.SiteId IS NULL))
   AND t.ModuleKey = s.ModuleKey AND t.SettingKey = s.SettingKey
WHEN MATCHED THEN UPDATE SET
    IsSecret=0, PlainValue=@PlainValue, EncryptedValue=NULL, KeyVersion=NULL,
    UpdatedAt=SYSDATETIME(), UpdatedByUserId=@Actor
WHEN NOT MATCHED THEN INSERT (SiteId, ModuleKey, SettingKey, IsSecret, PlainValue, EncryptedValue, KeyVersion, UpdatedByUserId)
    VALUES (@SiteId, @ModuleKey, @SettingKey, 0, @PlainValue, NULL, NULL, @Actor);",
                new { SiteId = siteId, ModuleKey = moduleKey, SettingKey = settingKey, PlainValue = plainValue, Actor = actorUserId });
        }

        public async Task UpsertSecretAsync(int? siteId, string moduleKey, string settingKey, byte[] encrypted, int keyVersion, int? actorUserId)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(@"
MERGE koch.ModuleSettingValues AS t
USING (SELECT @SiteId AS SiteId, @ModuleKey AS ModuleKey, @SettingKey AS SettingKey) AS s
ON ((t.SiteId = s.SiteId) OR (t.SiteId IS NULL AND s.SiteId IS NULL))
   AND t.ModuleKey = s.ModuleKey AND t.SettingKey = s.SettingKey
WHEN MATCHED THEN UPDATE SET
    IsSecret=1, PlainValue=NULL, EncryptedValue=@Encrypted, KeyVersion=@KeyVersion,
    UpdatedAt=SYSDATETIME(), UpdatedByUserId=@Actor
WHEN NOT MATCHED THEN INSERT (SiteId, ModuleKey, SettingKey, IsSecret, PlainValue, EncryptedValue, KeyVersion, UpdatedByUserId)
    VALUES (@SiteId, @ModuleKey, @SettingKey, 1, NULL, @Encrypted, @KeyVersion, @Actor);",
                new { SiteId = siteId, ModuleKey = moduleKey, SettingKey = settingKey, Encrypted = encrypted, KeyVersion = keyVersion, Actor = actorUserId });
        }

        public async Task WriteAuditAsync(SecretAuditEntry entry)
        {
            if (entry == null) return;
            try
            {
                using var conn = _factory.CreateConnection();
                await conn.ExecuteAsync(@"
INSERT INTO koch.SecretAuditLog (Action, ModuleKey, SettingKey, SiteId, ActorUserId, ActorRole, ClientIp, UserAgent, KeyVersion, Success)
VALUES (@Action, @ModuleKey, @SettingKey, @SiteId, @ActorUserId, @ActorRole, @ClientIp, @UserAgent, @KeyVersion, @Success)",
                    entry);
            }
            catch
            {
                /* جدول ممکن است هنوز ساخته نشده باشد */
            }
        }

        public async Task<IReadOnlyList<ModuleSettingDefinition>> ListDefinitionsAsync()
        {
            using var conn = _factory.CreateConnection();
            var rows = await conn.QueryAsync<ModuleSettingDefinition>(
                @"SELECT ModuleKey, SettingKey, DisplayName, GroupName, ValueKind, IsSecret, FallbackPolicy,
                         LegacyConfigPath, AllowEmpty, SortOrder, IsActive
                  FROM koch.ModuleSettingDefinitions
                  WHERE IsActive=1
                  ORDER BY ModuleKey, SortOrder, SettingKey");
            return rows.AsList();
        }

        public async Task EnsureDefinitionAsync(ModuleSettingDefinition definition)
        {
            if (definition == null || string.IsNullOrWhiteSpace(definition.ModuleKey) || string.IsNullOrWhiteSpace(definition.SettingKey))
                return;
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(@"
IF NOT EXISTS (
    SELECT 1 FROM koch.ModuleSettingDefinitions
    WHERE ModuleKey=@ModuleKey AND SettingKey=@SettingKey)
    INSERT INTO koch.ModuleSettingDefinitions
        (ModuleKey, SettingKey, DisplayName, GroupName, ValueKind, IsSecret, FallbackPolicy, LegacyConfigPath, AllowEmpty, SortOrder, IsActive)
    VALUES
        (@ModuleKey, @SettingKey, @DisplayName, @GroupName, @ValueKind, @IsSecret, @FallbackPolicy, @LegacyConfigPath, @AllowEmpty, @SortOrder, 1);",
                new
                {
                    definition.ModuleKey,
                    definition.SettingKey,
                    DisplayName = string.IsNullOrWhiteSpace(definition.DisplayName) ? definition.SettingKey : definition.DisplayName,
                    definition.GroupName,
                    ValueKind = string.IsNullOrWhiteSpace(definition.ValueKind) ? "string" : definition.ValueKind,
                    IsSecret = definition.IsSecret ? 1 : 0,
                    FallbackPolicy = string.IsNullOrWhiteSpace(definition.FallbackPolicy)
                        ? SettingFallbackPolicies.LegacyConfigUntilMigrated
                        : definition.FallbackPolicy,
                    definition.LegacyConfigPath,
                    AllowEmpty = definition.AllowEmpty ? 1 : 0,
                    SortOrder = definition.SortOrder
                });
        }

        public async Task<bool> HasSecretStoredAsync(int? siteId, string moduleKey, string settingKey)
        {
            using var conn = _factory.CreateConnection();
            var n = siteId.HasValue && siteId.Value > 0
                ? await conn.ExecuteScalarAsync<int>(
                    @"SELECT COUNT(1) FROM koch.ModuleSettingValues
                      WHERE SiteId=@SiteId AND ModuleKey=@ModuleKey AND SettingKey=@SettingKey
                        AND IsSecret=1 AND EncryptedValue IS NOT NULL",
                    new { SiteId = siteId.Value, ModuleKey = moduleKey, SettingKey = settingKey })
                : await conn.ExecuteScalarAsync<int>(
                    @"SELECT COUNT(1) FROM koch.ModuleSettingValues
                      WHERE SiteId IS NULL AND ModuleKey=@ModuleKey AND SettingKey=@SettingKey
                        AND IsSecret=1 AND EncryptedValue IS NOT NULL",
                    new { ModuleKey = moduleKey, SettingKey = settingKey });
            return n > 0;
        }

        public async Task<bool> GetWebhookEnforceAsync(int siteId)
        {
            if (siteId <= 0) return false;
            using var conn = _factory.CreateConnection();
            var v = await conn.ExecuteScalarAsync<bool?>(
                "SELECT WebhookEnforce FROM koch.SiteSecurityFlags WHERE SiteId=@SiteId",
                new { SiteId = siteId });
            return v == true;
        }

        public async Task SetWebhookEnforceAsync(int siteId, bool enforce)
        {
            if (siteId <= 0) return;
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(@"
MERGE koch.SiteSecurityFlags AS t
USING (SELECT @SiteId AS SiteId) AS s ON t.SiteId = s.SiteId
WHEN MATCHED THEN UPDATE SET WebhookEnforce=@Enforce, UpdatedAt=SYSDATETIME()
WHEN NOT MATCHED THEN INSERT (SiteId, AllowPlatformSecretFallback, WebhookEnforce)
VALUES (@SiteId, 0, @Enforce);",
                new { SiteId = siteId, Enforce = enforce });
        }
    }
}
