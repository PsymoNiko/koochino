using MyShop.Core.Models.Dashboard;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Interfaces.Dashboard
{
    public interface IDashboardWidgetRepository
    {
        Task<List<DashboardWidgetDef>> ListCatalogAsync(string panelType);
        Task<List<UserDashboardWidget>> GetLayoutAsync(int userId, string panelType);
        Task SaveLayoutAsync(int userId, string panelType, IEnumerable<UserDashboardWidget> items);
    }

    public interface IDashboardWidgetService
    {
        Task<List<UserDashboardWidget>> GetEffectiveLayoutAsync(int userId, string panelType);
        Task SaveLayoutAsync(int userId, string panelType, IEnumerable<UserDashboardWidget> items);
        Task<List<DashboardWidgetDef>> GetCatalogAsync(string panelType);
    }

    public interface IHomeSectionRepository
    {
        Task<List<HomeSection>> ListAsync(int siteId, bool visibleOnly = false);
        Task<HomeSection> GetAsync(int id);
        Task<int> UpsertAsync(HomeSection section);
        Task DeleteAsync(int id);
    }

    public interface IHomeSectionService
    {
        Task<List<HomeSection>> ListAsync(int siteId, bool visibleOnly = false);
        Task<int> UpsertAsync(HomeSection section);
        Task DeleteAsync(int id);
    }
}
