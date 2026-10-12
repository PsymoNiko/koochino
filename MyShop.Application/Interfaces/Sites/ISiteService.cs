using MyShop.Core.Models.Sites;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Interfaces.Sites
{
    public interface ISiteRepository
    {
        Task<List<Site>> ListAsync(bool activeOnly = false);
        Task<Site> GetByIdAsync(int id);
        Task<Site> GetByCodeAsync(string code);
        Task<ResolvedSite> ResolveByHostAsync(string host);
        Task<int> UpsertAsync(Site site);
        Task SetDomainsAsync(int siteId, IEnumerable<SiteDomain> domains);
        Task<List<int>> GetAdminSiteIdsAsync(int userId);
        Task SetAdminSitesAsync(int userId, IEnumerable<int> siteIds);
        Task<bool> AdminHasSiteAccessAsync(int userId, int siteId);
    }

    public interface ISiteService
    {
        Task<List<Site>> ListAsync(bool activeOnly = false);
        Task<Site> GetByIdAsync(int id);
        Task<Site> GetByCodeAsync(string code);
        Task<ResolvedSite> ResolveByHostAsync(string host, string connectionStringHint = null);
        Task<int> UpsertAsync(Site site);
        Task SetDomainsAsync(int siteId, IEnumerable<SiteDomain> domains);
        Task<List<int>> GetAdminSiteIdsAsync(int userId);
        Task SetAdminSitesAsync(int userId, IEnumerable<int> siteIds);
        Task<bool> AdminCanAccessSiteAsync(int userId, int siteId);
        Task<SiteBrandCopy> GetBrandAsync(int siteId);
    }
}
