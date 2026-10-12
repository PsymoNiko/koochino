using MyShop.Core.Models.Cms;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Interfaces.Cms
{
    public interface ISitePageContentRepository
    {
        Task<SitePageContent> GetAsync(int siteId, string pageKey);
        Task<List<SitePageContent>> ListBySiteAsync(int siteId);
        Task UpsertAsync(SitePageContent row);
        Task DeleteAsync(int siteId, string pageKey);
    }

    public interface ISitePageContentService
    {
        IReadOnlyList<SitePageInfo> GetPublicPageCatalog();
        Task<LandingGreenCopy> GetLandingGreenAsync(int? siteId);
        Task SaveLandingGreenAsync(int siteId, LandingGreenCopy copy, int? updatedBy);
        Task<MissionPageCopy> GetMissionAsync(int? siteId);
        Task SaveMissionAsync(int siteId, MissionPageCopy copy, int? updatedBy);
        Task<StorePrintCopy> GetStorePrintAsync(int? siteId);
        Task SaveStorePrintAsync(int siteId, StorePrintCopy copy, int? updatedBy);
        Task<SitePageContent> GetRawAsync(int siteId, string pageKey);
    }
}
