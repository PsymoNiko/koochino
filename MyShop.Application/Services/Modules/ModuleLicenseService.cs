using MyShop.Application.Interfaces.Modules;
using MyShop.Application.Interfaces.Sites;
using MyShop.Core.Models.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Application.Services.Modules
{
    public class ModuleLicenseService : IModuleLicenseService
    {
        private readonly ISiteModuleRepository _repo;
        private readonly ISiteModuleService _siteModules;
        private readonly ILicenseCallerContext _caller;
        private readonly ISiteService _sites;

        public ModuleLicenseService(
            ISiteModuleRepository repo,
            ISiteModuleService siteModules,
            ILicenseCallerContext caller = null,
            ISiteService sites = null)
        {
            _repo = repo;
            _siteModules = siteModules;
            _caller = caller;
            _sites = sites;
        }

        public async Task<IReadOnlyList<ModuleCatalogItem>> GetCatalogAsync(bool sellableOnly = true)
        {
            try
            {
                return await _repo.GetCatalogAsync(activeOnly: true, sellableOnly: sellableOnly);
            }
            catch
            {
                return Array.Empty<ModuleCatalogItem>();
            }
        }

        public async Task<IReadOnlyList<SiteModuleEntitlement>> GetLicensedModulesAsync(int siteId)
        {
            if (!await CanManageAsync(siteId))
                return Array.Empty<SiteModuleEntitlement>();
            var all = await _siteModules.ListForSiteAsync(siteId);
            return all
                .Where(x => string.Equals(x.Source, ModuleSources.Licensed, StringComparison.OrdinalIgnoreCase)
                            || string.Equals(x.Source, ModuleSources.Trial, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        public async Task ActivateLicenseAsync(int siteId, string moduleKey, DateTime? expiresAt = null)
        {
            if (siteId <= 0 || string.IsNullOrWhiteSpace(moduleKey)) return;
            await EnsureCanManageAsync(siteId);
            await _repo.UpsertAsync(siteId, moduleKey.Trim(), isEnabled: true, source: ModuleSources.Licensed, expiresAt: expiresAt);
            _siteModules.InvalidateCache(siteId);
        }

        public async Task DeactivateLicenseAsync(int siteId, string moduleKey)
        {
            if (siteId <= 0 || string.IsNullOrWhiteSpace(moduleKey)) return;
            await EnsureCanManageAsync(siteId);
            await _repo.UpsertAsync(siteId, moduleKey.Trim(), isEnabled: false, source: ModuleSources.Licensed, expiresAt: null);
            _siteModules.InvalidateCache(siteId);
        }

        async Task EnsureCanManageAsync(int siteId)
        {
            if (await CanManageAsync(siteId)) return;
            throw new UnauthorizedAccessException("اجازه مدیریت لایسنس این سایت را ندارید.");
        }

        async Task<bool> CanManageAsync(int siteId)
        {
            if (siteId <= 0 || _caller == null)
                return false;
            if (!_caller.IsAuthenticated || !_caller.UserId.HasValue)
                return false;

            if (_caller.IsAdmin && _sites != null)
                return await _sites.AdminCanAccessSiteAsync(_caller.UserId.Value, siteId);

            if (_caller.IsStoreOwner && _caller.TenantSiteId.HasValue && _caller.TenantSiteId.Value == siteId)
                return true;

            return false;
        }
    }
}
