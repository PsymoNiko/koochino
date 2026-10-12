using MyShop.Core.Models.Modules;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Interfaces.Modules
{
    public interface IModuleRegistryService
    {
        /// <summary>همه ماژول‌های فعال از کاتالوگ (با fallback به لیست پیش‌فرض).</summary>
        Task<IReadOnlyList<ModuleCatalogItem>> GetAdminModulesAsync();

        /// <summary>ماژول‌های قابل تخصیص به فروشنده‌دار.</summary>
        Task<IReadOnlyList<ModuleCatalogItem>> GetStoreOwnerModulesAsync();

        Task<string> GetDisplayNameAsync(string moduleKey);
    }
}
