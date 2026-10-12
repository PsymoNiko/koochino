using MyShop.Core.Models.Settings;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Interfaces.Settings
{
    public interface ISiteAdminSettingsService
    {
        Task<SiteAdminSettingsDto> GetSettingsAsync(int siteId, string defaultBrand = "کوچینو");
        Task SaveSettingsAsync(int siteId, SiteAdminSettingsDto settings, int? updatedBy);
        Task<TermsPageDto> GetTermsAsync(int? siteId, string defaultBrand = "کوچینو", bool forStore = false);
        Task SaveTermsAsync(int siteId, TermsPageDto terms, int? updatedBy, bool forStore = false);
    }

    public interface IAppVersionService
    {
        AppVersionManifest GetManifest();
        Task<List<PlatformReleaseNote>> ListReleaseNotesAsync(int take = 30);
        Task<int> AddReleaseNoteAsync(PlatformReleaseNote note);
        Task DeleteReleaseNoteAsync(int id);
    }

    public interface IPlatformContextService
    {
        bool IsSuperAdminHost();
        bool CanManageAllSites(int adminUserId, bool isFullAccess);
        string CurrentHost { get; }
    }
}
