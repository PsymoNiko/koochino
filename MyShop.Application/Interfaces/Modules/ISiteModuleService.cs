using MyShop.Core.Models.Modules;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Interfaces.Modules
{
    public interface ISiteModuleRepository
    {
        Task<IReadOnlyList<ModuleCatalogItem>> GetCatalogAsync(bool activeOnly = true, bool sellableOnly = false);
        Task<IReadOnlyList<SiteModuleEntitlement>> ListForSiteAsync(int siteId);
        Task<SiteModuleEntitlement> GetAsync(int siteId, string moduleKey);
        Task UpsertAsync(int siteId, string moduleKey, bool isEnabled, string source, DateTime? expiresAt);
        Task UpsertManyAsync(int siteId, IEnumerable<(string ModuleKey, bool IsEnabled, string Source)> items);
        Task SetEnabledAsync(int siteId, string moduleKey, bool isEnabled);
        Task DeleteAsync(int siteId, string moduleKey);
    }

    public interface ISiteModuleService
    {
        Task<bool> IsEnabledAsync(int siteId, string moduleKey, bool globalFallback = true);
        Task<IReadOnlyDictionary<string, bool>> GetEffectiveFlagsAsync(int siteId, IReadOnlyDictionary<string, bool> globalFallback);
        Task<IReadOnlyList<SiteModuleEntitlement>> ListForSiteAsync(int siteId);
        Task SaveSiteFlagsAsync(int siteId, IReadOnlyDictionary<string, bool> values, string source = ModuleSources.Core);
        void InvalidateCache(int siteId);
    }

    public interface IModuleLicenseService
    {
        Task<IReadOnlyList<ModuleCatalogItem>> GetCatalogAsync(bool sellableOnly = true);
        Task<IReadOnlyList<SiteModuleEntitlement>> GetLicensedModulesAsync(int siteId);
        Task ActivateLicenseAsync(int siteId, string moduleKey, DateTime? expiresAt = null);
        Task DeactivateLicenseAsync(int siteId, string moduleKey);
    }

    /// <summary>هویت فراخوان لایسنس (از HttpContext در Web/Api).</summary>
    public interface ILicenseCallerContext
    {
        bool IsAuthenticated { get; }
        int? UserId { get; }
        bool IsAdmin { get; }
        bool IsStoreOwner { get; }
        int? TenantSiteId { get; }
    }
}
