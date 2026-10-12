using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using MyShop.Application.Interfaces.Security;
using MyShop.Core.Models.Security;

namespace MyShop.Application.Services.Security
{
    public class ModuleConfigService : IModuleConfigService
    {
        readonly IModuleSettingRepository _repo;
        readonly IEncryptionService _encryption;
        readonly IConfiguration _config;
        readonly IRequestAuditContext _audit;

        public ModuleConfigService(
            IModuleSettingRepository repo,
            IEncryptionService encryption,
            IConfiguration config,
            IRequestAuditContext audit = null)
        {
            _repo = repo;
            _encryption = encryption;
            _config = config;
            _audit = audit;
        }

        public Task<string> GetStringAsync(int? siteId, string moduleKey, string settingKey, bool allowLegacyFallback = true)
            => ResolveAsync(siteId, moduleKey, settingKey, wantSecret: false, allowLegacyFallback);

        public Task<string> GetSecretAsync(int? siteId, string moduleKey, string settingKey, bool allowLegacyFallback = true)
            => ResolveAsync(siteId, moduleKey, settingKey, wantSecret: true, allowLegacyFallback);

        public async Task SetStringAsync(int? siteId, string moduleKey, string settingKey, string value)
        {
            var def = await _repo.GetDefinitionAsync(moduleKey, settingKey);
            if (def == null) throw new InvalidOperationException("تعریف تنظیم یافت نشد.");
            if (def.IsSecret) throw new InvalidOperationException("این کلید secret است و نباید Plain ذخیره شود.");
            if (!CanAccessSite(siteId, write: true))
            {
                await AuditAsync("Set", moduleKey, settingKey, siteId, null, false);
                throw new UnauthorizedAccessException("اجازه ذخیره تنظیم این سایت را ندارید.");
            }
            await _repo.UpsertPlainAsync(NormalizeSite(siteId), moduleKey, settingKey, value, _audit?.UserId);
            await AuditAsync("Set", moduleKey, settingKey, siteId, null, true);
        }

        public async Task SetSecretAsync(int? siteId, string moduleKey, string settingKey, string secret)
        {
            var def = await _repo.GetDefinitionAsync(moduleKey, settingKey);
            if (def == null) throw new InvalidOperationException("تعریف تنظیم یافت نشد.");
            if (!def.IsSecret) throw new InvalidOperationException("این کلید secret نیست.");
            if (!_encryption.HasMasterKey)
                throw new InvalidOperationException("کلید رمزنگاری تنظیم نشده است؛ secret ذخیره نشد.");
            if (!CanAccessSite(siteId, write: true))
            {
                await AuditAsync("Set", moduleKey, settingKey, siteId, null, false);
                throw new UnauthorizedAccessException("اجازه ذخیره secret این سایت را ندارید.");
            }
            var blob = _encryption.Encrypt(secret ?? "", out var ver);
            await _repo.UpsertSecretAsync(NormalizeSite(siteId), moduleKey, settingKey, blob, ver, _audit?.UserId);
            await AuditAsync("Set", moduleKey, settingKey, siteId, ver, true);
        }

        async Task<string> ResolveAsync(int? siteId, string moduleKey, string settingKey, bool wantSecret, bool allowLegacyFallback = true)
        {
            ModuleSettingDefinition def = null;
            try { def = await _repo.GetDefinitionAsync(moduleKey, settingKey); }
            catch { def = null; }
            if (def == null) return null;

            if (wantSecret && !CanAccessSite(siteId, write: false))
            {
                await AuditAsync("Read", moduleKey, settingKey, siteId, null, false);
                return null;
            }

            var siteRow = await TryGetRowAsync(NormalizeSite(siteId), moduleKey, settingKey);
            var fromSite = Decode(siteRow, wantSecret);
            if (fromSite != null) return fromSite;

            var policy = def.FallbackPolicy ?? SettingFallbackPolicies.None;
            if (string.Equals(policy, SettingFallbackPolicies.PlatformIfAllowed, StringComparison.OrdinalIgnoreCase)
                && siteId.HasValue && siteId.Value > 0)
            {
                var allow = false;
                try { allow = await _repo.GetAllowPlatformSecretFallbackAsync(siteId.Value); } catch { }
                if (allow)
                {
                    var plat = Decode(await TryGetRowAsync(null, moduleKey, settingKey), wantSecret);
                    if (plat != null) return plat;
                }
            }

            // کلیدهای سابق appsettings به‌صورت پلتفرم (SiteId NULL) ذخیره می‌شوند.
            if (string.Equals(policy, SettingFallbackPolicies.LegacyConfigUntilMigrated, StringComparison.OrdinalIgnoreCase))
            {
                if (siteId.HasValue && siteId.Value > 0)
                {
                    var plat = Decode(await TryGetRowAsync(null, moduleKey, settingKey), wantSecret);
                    if (plat != null) return plat;
                }
                if (allowLegacyFallback && !string.IsNullOrWhiteSpace(def.LegacyConfigPath))
                    return _config[def.LegacyConfigPath];
            }

            return null;
        }

        async Task<ModuleSettingValueRow> TryGetRowAsync(int? siteId, string moduleKey, string settingKey)
        {
            try { return await _repo.GetValueAsync(siteId, moduleKey, settingKey); }
            catch { return null; }
        }

        string Decode(ModuleSettingValueRow row, bool wantSecret)
        {
            if (row == null) return null;
            if (row.IsSecret)
            {
                if (!wantSecret) return null;
                if (row.EncryptedValue == null || !row.KeyVersion.HasValue) return null;
                try { return _encryption.Decrypt(row.EncryptedValue, row.KeyVersion.Value); }
                catch { return null; }
            }
            return wantSecret ? null : row.PlainValue;
        }

        bool CanAccessSite(int? siteId, bool write)
        {
            if (_audit == null) return !write;
            if (!siteId.HasValue || siteId.Value <= 0) return _audit.IsAdmin || !write;
            if (_audit.IsAdmin) return true;
            return _audit.TenantSiteId.HasValue && _audit.TenantSiteId.Value == siteId.Value;
        }

        static int? NormalizeSite(int? siteId) => siteId.HasValue && siteId.Value > 0 ? siteId : null;

        Task AuditAsync(string action, string module, string key, int? siteId, int? keyVersion, bool success)
        {
            return _repo.WriteAuditAsync(new SecretAuditEntry
            {
                Action = action,
                ModuleKey = module,
                SettingKey = key,
                SiteId = siteId,
                ActorUserId = _audit?.UserId,
                ActorRole = _audit?.ActorRole,
                ClientIp = _audit?.ClientIp,
                UserAgent = _audit?.UserAgent,
                KeyVersion = keyVersion,
                Success = success
            });
        }
    }
}
