using MyShop.Application.Interfaces.Cms;
using MyShop.Application.Interfaces.Settings;
using MyShop.Core.Helpers;
using MyShop.Core.Models.Cms;
using MyShop.Core.Models.Settings;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace MyShop.Application.Services.Settings
{
    public class SiteAdminSettingsService : ISiteAdminSettingsService
    {
        public const string KeyAdminSettings = "admin_settings";
        public const string KeyTerms = "terms";
        public const string KeyTermsStore = "terms_store";

        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        private readonly ISitePageContentRepository _repo;

        public SiteAdminSettingsService(ISitePageContentRepository repo) => _repo = repo;

        public async Task<SiteAdminSettingsDto> GetSettingsAsync(int siteId, string defaultBrand = "کوچینو")
        {
            var def = SiteAdminSettingsDto.GetDefault(defaultBrand);
            if (siteId <= 0) return def;
            try
            {
                var row = await _repo.GetAsync(siteId, KeyAdminSettings);
                if (row == null || string.IsNullOrWhiteSpace(row.ContentJson)) return def;
                var parsed = JsonSerializer.Deserialize<SiteAdminSettingsDto>(row.ContentJson, JsonOpts);
                if (parsed == null) return def;
                if (string.IsNullOrWhiteSpace(parsed.SiteDisplayName)) parsed.SiteDisplayName = defaultBrand;
                if (string.IsNullOrWhiteSpace(parsed.TermsPageRoute)) parsed.TermsPageRoute = "/terms";
                if (string.IsNullOrWhiteSpace(parsed.StoreTermsPageRoute)) parsed.StoreTermsPageRoute = "/terms/store";
                if (parsed.VoucherCreditExpireDays < 1 || parsed.VoucherCreditExpireDays > 730)
                    parsed.VoucherCreditExpireDays = 90;
                return parsed;
            }
            catch { return def; }
        }

        public async Task SaveSettingsAsync(int siteId, SiteAdminSettingsDto settings, int? updatedBy)
        {
            if (siteId <= 0 || settings == null) return;
            if (settings.VoucherCreditExpireDays < 1) settings.VoucherCreditExpireDays = 1;
            if (settings.VoucherCreditExpireDays > 730) settings.VoucherCreditExpireDays = 730;
            await _repo.UpsertAsync(new SitePageContent
            {
                SiteId = siteId,
                PageKey = KeyAdminSettings,
                Title = "تنظیمات پنل",
                ContentJson = JsonSerializer.Serialize(settings, JsonOpts),
                UpdatedBy = updatedBy
            });
        }

        public async Task<TermsPageDto> GetTermsAsync(int? siteId, string defaultBrand = "کوچینو", bool forStore = false)
        {
            var def = forStore ? TermsPageDto.GetDefaultStore() : TermsPageDto.GetDefaultKoochino();
            var key = forStore ? KeyTermsStore : KeyTerms;
            if (!siteId.HasValue || siteId.Value <= 0) return def;
            try
            {
                var row = await _repo.GetAsync(siteId.Value, key);
                if (row == null || string.IsNullOrWhiteSpace(row.ContentJson)) return def;
                var parsed = JsonSerializer.Deserialize<TermsPageDto>(row.ContentJson, JsonOpts);
                if (parsed == null) return def;
                if (string.IsNullOrWhiteSpace(parsed.HtmlBody)) parsed.HtmlBody = def.HtmlBody;
                if (string.IsNullOrWhiteSpace(parsed.Title)) parsed.Title = def.Title;
                return parsed;
            }
            catch { return def; }
        }

        public async Task SaveTermsAsync(int siteId, TermsPageDto terms, int? updatedBy, bool forStore = false)
        {
            if (siteId <= 0)
                throw new InvalidOperationException("سایت شناسایی نشد؛ ذخیره انجام نشد.");
            if (terms == null)
                throw new ArgumentNullException(nameof(terms));
            terms.HtmlBody = HtmlSanitize.Sanitize(terms.HtmlBody);
            await _repo.UpsertAsync(new SitePageContent
            {
                SiteId = siteId,
                PageKey = forStore ? KeyTermsStore : KeyTerms,
                Title = terms.Title ?? (forStore ? "قوانین مغازه‌داران" : "قوانین مشتریان"),
                ContentJson = JsonSerializer.Serialize(terms, JsonOpts),
                UpdatedBy = updatedBy
            });
        }
    }
}
