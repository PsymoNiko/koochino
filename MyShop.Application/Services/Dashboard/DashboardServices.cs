using MyShop.Application.Interfaces.Dashboard;
using MyShop.Core.Models.Dashboard;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Application.Services.Dashboard
{
    public class DashboardWidgetService : IDashboardWidgetService
    {
        private readonly IDashboardWidgetRepository _repo;

        public DashboardWidgetService(IDashboardWidgetRepository repo) => _repo = repo;

        public Task<List<DashboardWidgetDef>> GetCatalogAsync(string panelType) =>
            _repo.ListCatalogAsync(panelType);

        public async Task<List<UserDashboardWidget>> GetEffectiveLayoutAsync(int userId, string panelType)
        {
            var catalog = await _repo.ListCatalogAsync(panelType);
            if (catalog.Count == 0)
            {
                catalog = DefaultCatalog(panelType);
            }
            else
            {
                foreach (var d in DefaultCatalog(panelType))
                {
                    if (!catalog.Any(c => string.Equals(c.WidgetKey, d.WidgetKey, StringComparison.OrdinalIgnoreCase)))
                        catalog.Add(d);
                }
            }

            List<UserDashboardWidget> saved;
            try { saved = await _repo.GetLayoutAsync(userId, panelType); }
            catch { saved = new List<UserDashboardWidget>(); }

            var byKey = saved.ToDictionary(x => x.WidgetKey, StringComparer.OrdinalIgnoreCase);
            var result = new List<UserDashboardWidget>();
            foreach (var c in catalog.OrderBy(x => x.DefaultOrder))
            {
                if (byKey.TryGetValue(c.WidgetKey, out var row))
                {
                    row.Title = c.Title;
                    result.Add(row);
                }
                else
                {
                    result.Add(new UserDashboardWidget
                    {
                        UserId = userId,
                        PanelType = panelType,
                        WidgetKey = c.WidgetKey,
                        Title = c.Title,
                        SortOrder = c.DefaultOrder,
                        IsVisible = DefaultVisible(c.WidgetKey)
                    });
                }
            }
            return result.OrderBy(x => x.SortOrder).ThenBy(x => x.WidgetKey).ToList();
        }

        public Task SaveLayoutAsync(int userId, string panelType, IEnumerable<UserDashboardWidget> items) =>
            _repo.SaveLayoutAsync(userId, panelType, items);

        private static List<DashboardWidgetDef> DefaultCatalog(string panelType)
        {
            if (string.Equals(panelType, "StoreOwner", StringComparison.OrdinalIgnoreCase))
            {
                return new List<DashboardWidgetDef>
                {
                    new() { WidgetKey = "so-summary", Title = "خلاصه فروشنده", DefaultOrder = 10, PanelType = panelType },
                    new() { WidgetKey = "so-orders", Title = "سفارشات فروشنده", DefaultOrder = 20, PanelType = panelType }
                };
            }
            return new List<DashboardWidgetDef>
            {
                new() { WidgetKey = "commerce-kpis", Title = "فروشگاه آنلاین", DefaultOrder = 10, PanelType = "Admin" },
                new() { WidgetKey = "sales", Title = "فروش", DefaultOrder = 20, PanelType = "Admin" },
                new() { WidgetKey = "orders", Title = "سفارشات", DefaultOrder = 30, PanelType = "Admin" },
                new() { WidgetKey = "coupons", Title = "کوپن‌ها", DefaultOrder = 40, PanelType = "Admin" },
                new() { WidgetKey = "chancebox", Title = "جعبه‌شانس", DefaultOrder = 50, PanelType = "Admin" },
                new() { WidgetKey = "users", Title = "کاربران", DefaultOrder = 60, PanelType = "Admin" },
                new() { WidgetKey = "tickets", Title = "تیکت‌ها", DefaultOrder = 70, PanelType = "Admin" },
                new() { WidgetKey = "live-chat", Title = "چت آنلاین پشتیبانی", DefaultOrder = 75, PanelType = "Admin" },
                new() { WidgetKey = "system", Title = "سلامت سیستم", DefaultOrder = 80, PanelType = "Admin" },
                new() { WidgetKey = "projects", Title = "پروژه‌ها", DefaultOrder = 90, PanelType = "Admin" },
                new() { WidgetKey = "points-loyalty", Title = "امتیازها و ماموریت‌ها", DefaultOrder = 100, PanelType = "Admin" }
            };
        }

        private static bool DefaultVisible(string widgetKey) =>
            !string.Equals(widgetKey, "points-loyalty", StringComparison.OrdinalIgnoreCase);
    }

    public class HomeSectionService : IHomeSectionService
    {
        private readonly IHomeSectionRepository _repo;
        public HomeSectionService(IHomeSectionRepository repo) => _repo = repo;
        public Task<List<HomeSection>> ListAsync(int siteId, bool visibleOnly = false) => _repo.ListAsync(siteId, visibleOnly);
        public Task<int> UpsertAsync(HomeSection section) => _repo.UpsertAsync(section);
        public Task DeleteAsync(int id) => _repo.DeleteAsync(id);
    }
}
