using Dapper;
using MyShop.Application.Interfaces.Dashboard;
using MyShop.Core.Interfaces;
using MyShop.Core.Models.Dashboard;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Infrastructure.Repositories.Dashboard
{
    public class DashboardWidgetRepository : IDashboardWidgetRepository
    {
        private readonly IDbConnectionFactory _factory;

        public DashboardWidgetRepository(IDbConnectionFactory factory) => _factory = factory;

        public async Task<List<DashboardWidgetDef>> ListCatalogAsync(string panelType)
        {
            using var conn = _factory.CreateConnection();
            var rows = await conn.QueryAsync<DashboardWidgetDef>(@"
                SELECT WidgetKey, Title, ModuleKey, PanelType, DefaultOrder, IsActive
                FROM koch.DashboardWidgetCatalog
                WHERE IsActive = 1 AND PanelType = @PanelType
                ORDER BY DefaultOrder, WidgetKey",
                new { PanelType = panelType });
            return rows?.ToList() ?? new List<DashboardWidgetDef>();
        }

        public async Task<List<UserDashboardWidget>> GetLayoutAsync(int userId, string panelType)
        {
            using var conn = _factory.CreateConnection();
            var rows = await conn.QueryAsync<UserDashboardWidget>(@"
                SELECT u.Id, u.UserId, u.PanelType, u.WidgetKey, c.Title, u.SortOrder, u.IsVisible, u.ConfigJson
                FROM koch.UserDashboardLayout u
                LEFT JOIN koch.DashboardWidgetCatalog c ON c.WidgetKey = u.WidgetKey
                WHERE u.UserId = @UserId AND u.PanelType = @PanelType
                ORDER BY u.SortOrder, u.WidgetKey",
                new { UserId = userId, PanelType = panelType });
            return rows?.ToList() ?? new List<UserDashboardWidget>();
        }

        public async Task SaveLayoutAsync(int userId, string panelType, IEnumerable<UserDashboardWidget> items)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(
                "DELETE FROM koch.UserDashboardLayout WHERE UserId=@UserId AND PanelType=@PanelType",
                new { UserId = userId, PanelType = panelType });
            foreach (var item in items ?? Enumerable.Empty<UserDashboardWidget>())
            {
                await conn.ExecuteAsync(@"
                    INSERT INTO koch.UserDashboardLayout (UserId, PanelType, WidgetKey, SortOrder, IsVisible, ConfigJson)
                    VALUES (@UserId, @PanelType, @WidgetKey, @SortOrder, @IsVisible, @ConfigJson)",
                    new
                    {
                        UserId = userId,
                        PanelType = panelType,
                        item.WidgetKey,
                        item.SortOrder,
                        item.IsVisible,
                        item.ConfigJson
                    });
            }
        }
    }

    public class HomeSectionRepository : IHomeSectionRepository
    {
        private readonly IDbConnectionFactory _factory;

        public HomeSectionRepository(IDbConnectionFactory factory) => _factory = factory;

        public async Task<List<HomeSection>> ListAsync(int siteId, bool visibleOnly = false)
        {
            using var conn = _factory.CreateConnection();
            var rows = await conn.QueryAsync<HomeSection>(@"
                SELECT Id, SiteId, SectionKey, Title, ContentJson, LayoutType, SortOrder, IsVisible, UpdatedAt
                FROM koch.HomeSections
                WHERE SiteId = @SiteId AND (@VisibleOnly = 0 OR IsVisible = 1)
                ORDER BY SortOrder, Id",
                new { SiteId = siteId, VisibleOnly = visibleOnly ? 1 : 0 });
            return rows?.ToList() ?? new List<HomeSection>();
        }

        public async Task<HomeSection> GetAsync(int id)
        {
            using var conn = _factory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<HomeSection>(
                "SELECT * FROM koch.HomeSections WHERE Id=@Id", new { Id = id });
        }

        public async Task<int> UpsertAsync(HomeSection section)
        {
            using var conn = _factory.CreateConnection();
            if (section.Id > 0)
            {
                await conn.ExecuteAsync(@"
                    UPDATE koch.HomeSections
                    SET SectionKey=@SectionKey, Title=@Title, ContentJson=@ContentJson, LayoutType=@LayoutType,
                        SortOrder=@SortOrder, IsVisible=@IsVisible, UpdatedAt=SYSDATETIME()
                    WHERE Id=@Id", section);
                return section.Id;
            }
            return await conn.ExecuteScalarAsync<int>(@"
                INSERT INTO koch.HomeSections (SiteId, SectionKey, Title, ContentJson, LayoutType, SortOrder, IsVisible)
                VALUES (@SiteId, @SectionKey, @Title, @ContentJson, @LayoutType, @SortOrder, @IsVisible);
                SELECT CAST(SCOPE_IDENTITY() AS INT);", section);
        }

        public async Task DeleteAsync(int id)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync("DELETE FROM koch.HomeSections WHERE Id=@Id", new { Id = id });
        }
    }
}
