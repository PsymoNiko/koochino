using MyShop.Application.Interfaces.Modules;
using MyShop.Core.Models.Inventory;
using MyShop.Core.Models.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Application.Services.Modules
{
    public class ModuleRegistryService : IModuleRegistryService
    {
        private static readonly string[] FallbackAdminKeys =
        {
            "Users", "Stores", "Blog", "Shop", "Commerce", "Points", "LuckyLeague", "Announcements", "Common", "SocialMedia",
            "Companies", "Sliders", "Products", "Personnel", "ProjectManagement", "Inventory",
            "CRM", "Tickets", "Reports", "Finance", "Contracts", "Assets", "Training",
            "Sms", "AiAssistant", "Contact"
        };

        private static readonly Dictionary<string, string> FallbackNames = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Users"] = "کاربران",
            ["Stores"] = "فروشگاه‌ها",
            ["Blog"] = "بلاگ",
            ["Shop"] = "فروش",
            ["Commerce"] = "فروشگاه آنلاین",
            ["Points"] = "امتیاز",
            ["LuckyLeague"] = "جعبه شانس لیگی",
            ["Announcements"] = "اعلانات",
            ["Common"] = "پیام‌رسان",
            ["SocialMedia"] = "شبکه‌های اجتماعی",
            ["Companies"] = "شرکت‌ها",
            ["Sliders"] = "اسلایدر",
            ["Products"] = "محصولات",
            ["Personnel"] = "پرسونل",
            ["ProjectManagement"] = "مدیریت پروژه",
            ["Inventory"] = "انبار",
            ["CRM"] = "CRM",
            ["Tickets"] = "تیکت",
            ["Reports"] = "گزارش‌ها",
            ["Finance"] = "مالی",
            ["Contracts"] = "قراردادها",
            ["Assets"] = "دارایی‌ها",
            ["Training"] = "آموزش",
            ["Sms"] = "پیامک",
            ["AiAssistant"] = "دستیار هوشمند",
            ["Contact"] = "ارتباط",
            ["Orders"] = "سفارشات",
            ["Coupons"] = "کوپن‌ها",
            ["ChanceBox"] = "جعبه‌شانس",
            ["Documents"] = "مدارک"
        };

        private readonly ISiteModuleRepository _repo;

        public ModuleRegistryService(ISiteModuleRepository repo)
        {
            _repo = repo;
        }

        public async Task<IReadOnlyList<ModuleCatalogItem>> GetAdminModulesAsync()
        {
            try
            {
                var catalog = await _repo.GetCatalogAsync(activeOnly: true);
                if (catalog != null && catalog.Count > 0)
                {
                    // merge any missing fallback keys (e.g. newly coded modules not yet seeded)
                    var keys = new HashSet<string>(catalog.Select(c => c.ModuleKey), StringComparer.OrdinalIgnoreCase);
                    var merged = catalog.ToList();
                    foreach (var key in FallbackAdminKeys)
                    {
                        if (keys.Contains(key)) continue;
                        merged.Add(MakeFallback(key));
                    }
                    return merged.OrderBy(m => m.SortOrder).ThenBy(m => m.ModuleKey).ToList();
                }
            }
            catch { /* ModuleCatalog may not exist yet */ }

            return FallbackAdminKeys.Select(MakeFallback).ToList();
        }

        public async Task<IReadOnlyList<ModuleCatalogItem>> GetStoreOwnerModulesAsync()
        {
            var preferred = StoreOwnerModules.All.ToList();
            // ensure Commerce is available for store owners even if static list lagged
            if (!preferred.Any(k => string.Equals(k, "Commerce", StringComparison.OrdinalIgnoreCase)))
                preferred.Add("Commerce");

            try
            {
                var catalog = await _repo.GetCatalogAsync(activeOnly: true);
                var byKey = (catalog ?? Array.Empty<ModuleCatalogItem>())
                    .ToDictionary(c => c.ModuleKey, StringComparer.OrdinalIgnoreCase);

                var list = new List<ModuleCatalogItem>();
                foreach (var key in preferred)
                {
                    if (byKey.TryGetValue(key, out var item))
                    {
                        list.Add(item);
                        continue;
                    }
                    list.Add(MakeFallback(key));
                }
                return list;
            }
            catch
            {
                return preferred.Select(MakeFallback).ToList();
            }
        }

        public async Task<string> GetDisplayNameAsync(string moduleKey)
        {
            if (string.IsNullOrWhiteSpace(moduleKey)) return "";
            if (StoreOwnerModules.Labels.TryGetValue(moduleKey, out var so)) return so;
            if (FallbackNames.TryGetValue(moduleKey, out var fb)) return fb;
            try
            {
                var catalog = await _repo.GetCatalogAsync(activeOnly: false);
                var hit = catalog?.FirstOrDefault(c =>
                    string.Equals(c.ModuleKey, moduleKey, StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrWhiteSpace(hit?.DisplayName)) return hit.DisplayName;
            }
            catch { }
            return moduleKey;
        }

        private static ModuleCatalogItem MakeFallback(string key) => new()
        {
            ModuleKey = key,
            DisplayName = FallbackNames.TryGetValue(key, out var n) ? n : key,
            IsActive = true,
            SortOrder = Array.IndexOf(FallbackAdminKeys, key) is int i && i >= 0 ? i * 10 : 999
        };
    }
}
