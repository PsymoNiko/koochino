using MyShop.Application.Interfaces.Cms;
using MyShop.Core.Helpers;
using MyShop.Core.Models.Cms;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace MyShop.Application.Services.Cms
{
    public class SitePageContentService : ISitePageContentService
    {
        public const string PageHome = "home";
        public const string PageStores = "stores";
        public const string PageTerms = "terms";
        public const string PageTermsStore = "terms-store";
        public const string PageMission = "mission";
        public const string PageStorePrint = "store-print";

        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = false
        };

        private readonly ISitePageContentRepository _repo;

        public SitePageContentService(ISitePageContentRepository repo) => _repo = repo;

        public IReadOnlyList<SitePageInfo> GetPublicPageCatalog() => new List<SitePageInfo>
        {
            new()
            {
                PageKey = PageHome,
                Title = "صفحه اصلی",
                Route = "/",
                Description = "صفحه اصلی کوچینو — هیرو، چرخه وفاداری، امکانات، FAQ مشتری و مغازه‌دار، فوتر",
                HasStructuredEditor = true
            },
            new()
            {
                PageKey = PageStores,
                Title = "لیست فروشگاه‌ها",
                Route = "/stores",
                Description = "صفحه عمومی فروشگاه‌ها (عنوان/SEO از ماژول SEO؛ محتوا از داده فروشگاه‌ها)",
                HasStructuredEditor = false
            },
            new()
            {
                PageKey = PageTerms,
                Title = "قوانین ثبت‌نام مشتریان",
                Route = "/terms",
                Description = "متن قوانین مشتری — لینک ثبت‌نام مشتری",
                HasStructuredEditor = true
            },
            new()
            {
                PageKey = PageTermsStore,
                Title = "قوانین ثبت‌نام مغازه‌داران",
                Route = "/terms/store",
                Description = "متن قوانین مغازه‌دار — لینک ثبت‌نام فروشگاه",
                HasStructuredEditor = true
            },
            new()
            {
                PageKey = PageMission,
                Title = "ماموریت سایت",
                Route = "/mission",
                Description = "متن ماموریت عمومی سایت",
                HasStructuredEditor = true
            },
            new()
            {
                PageKey = "shop-home",
                Title = "خانه فروشگاه",
                Route = "/shop",
                Description = "بخش‌های داینامیک از «بخش‌های صفحه اول»؛ نوع چیدمان: Hero, ProductGrid, Banner, …",
                HasStructuredEditor = false
            }
        };

        public async Task<LandingGreenCopy> GetLandingGreenAsync(int? siteId)
        {
            var def = LandingGreenCopy.GetDefault();
            if (!siteId.HasValue || siteId.Value <= 0) return def;
            try
            {
                var row = await _repo.GetAsync(siteId.Value, PageHome);
                if (row == null || string.IsNullOrWhiteSpace(row.ContentJson)) return def;
                var parsed = JsonSerializer.Deserialize<LandingGreenCopy>(row.ContentJson, JsonOpts);
                return MergeLanding(def, parsed);
            }
            catch
            {
                return def;
            }
        }

        public async Task SaveLandingGreenAsync(int siteId, LandingGreenCopy copy, int? updatedBy)
        {
            if (copy == null) copy = LandingGreenCopy.GetDefault();
            await _repo.UpsertAsync(new SitePageContent
            {
                SiteId = siteId,
                PageKey = PageHome,
                Title = "صفحه اصلی",
                ContentJson = JsonSerializer.Serialize(copy, JsonOpts),
                UpdatedBy = updatedBy
            });
        }

        public async Task<MissionPageCopy> GetMissionAsync(int? siteId)
        {
            var def = MissionPageCopy.GetDefault();
            if (!siteId.HasValue || siteId.Value <= 0) return def;
            try
            {
                var row = await _repo.GetAsync(siteId.Value, PageMission);
                if (row == null || string.IsNullOrWhiteSpace(row.ContentJson)) return def;
                var parsed = JsonSerializer.Deserialize<MissionPageCopy>(row.ContentJson, JsonOpts);
                if (parsed == null) return def;
                if (string.IsNullOrWhiteSpace(parsed.Title)) parsed.Title = def.Title;
                if (string.IsNullOrWhiteSpace(parsed.Body)) parsed.Body = def.Body;
                return parsed;
            }
            catch { return def; }
        }

        public async Task SaveMissionAsync(int siteId, MissionPageCopy copy, int? updatedBy)
        {
            if (copy == null) copy = MissionPageCopy.GetDefault();
            copy.Body = HtmlSanitize.Sanitize(copy.Body);
            await _repo.UpsertAsync(new SitePageContent
            {
                SiteId = siteId,
                PageKey = PageMission,
                Title = copy.Title ?? "ماموریت",
                ContentJson = JsonSerializer.Serialize(copy, JsonOpts),
                UpdatedBy = updatedBy
            });
        }

        public async Task<StorePrintCopy> GetStorePrintAsync(int? siteId)
        {
            var def = StorePrintCopy.GetDefault();
            if (!siteId.HasValue || siteId.Value <= 0) return def;
            try
            {
                var row = await _repo.GetAsync(siteId.Value, PageStorePrint);
                if (row == null || string.IsNullOrWhiteSpace(row.ContentJson)) return def;
                var parsed = JsonSerializer.Deserialize<StorePrintCopy>(row.ContentJson, JsonOpts);
                return (parsed ?? def).CoalesceDefaults();
            }
            catch
            {
                return def;
            }
        }

        public async Task SaveStorePrintAsync(int siteId, StorePrintCopy copy, int? updatedBy)
        {
            if (copy == null) copy = StorePrintCopy.GetDefault();
            copy.CoalesceDefaults();
            await _repo.UpsertAsync(new SitePageContent
            {
                SiteId = siteId,
                PageKey = PageStorePrint,
                Title = "طرح چاپ فروشگاه",
                ContentJson = JsonSerializer.Serialize(copy, JsonOpts),
                UpdatedBy = updatedBy
            });
        }

        public Task<SitePageContent> GetRawAsync(int siteId, string pageKey) => _repo.GetAsync(siteId, pageKey);

        static LandingGreenCopy MergeLanding(LandingGreenCopy def, LandingGreenCopy parsed)
        {
            if (parsed == null) return def;
            if (parsed.FeatureCards == null || parsed.FeatureCards.Count == 0) parsed.FeatureCards = def.FeatureCards;
            if (parsed.ProcessSteps == null || parsed.ProcessSteps.Count == 0) parsed.ProcessSteps = def.ProcessSteps;
            if (parsed.CustomerFaqs == null || parsed.CustomerFaqs.Count == 0) parsed.CustomerFaqs = def.CustomerFaqs;
            if (parsed.StoreBenefitCards == null || parsed.StoreBenefitCards.Count == 0) parsed.StoreBenefitCards = def.StoreBenefitCards;
            if (parsed.StoreFaqs == null || parsed.StoreFaqs.Count == 0) parsed.StoreFaqs = def.StoreFaqs;
            if (string.IsNullOrWhiteSpace(parsed.BrandName)) parsed.BrandName = def.BrandName;
            if (string.IsNullOrWhiteSpace(parsed.CustomerHeroTitle)) parsed.CustomerHeroTitle = def.CustomerHeroTitle;

            // فیلدهای طراحی جدید صفحه اصلی — ردیف‌های قدیمی CMS این فیلدها را ندارند
            if (parsed.CycleSteps == null || parsed.CycleSteps.Count == 0) parsed.CycleSteps = def.CycleSteps;
            if (parsed.TrustPoints == null || parsed.TrustPoints.Count == 0) parsed.TrustPoints = def.TrustPoints;
            if (parsed.CustomerBenefits == null || parsed.CustomerBenefits.Count == 0) parsed.CustomerBenefits = def.CustomerBenefits;
            if (parsed.HeaderLoginButton == null) parsed.HeaderLoginButton = def.HeaderLoginButton;
            if (parsed.HeroBtnStore == null) parsed.HeroBtnStore = def.HeroBtnStore;
            if (parsed.HeroBtnCustomer == null) parsed.HeroBtnCustomer = def.HeroBtnCustomer;
            if (parsed.CustomerPanelButton == null) parsed.CustomerPanelButton = def.CustomerPanelButton;
            if (parsed.StorePanelButton == null) parsed.StorePanelButton = def.StorePanelButton;
            if (parsed.FinalCtaBtnStore == null) parsed.FinalCtaBtnStore = def.FinalCtaBtnStore;
            if (parsed.FinalCtaBtnCustomer == null) parsed.FinalCtaBtnCustomer = def.FinalCtaBtnCustomer;
            if (string.IsNullOrWhiteSpace(parsed.HeroTitleLine1)) parsed.HeroTitleLine1 = def.HeroTitleLine1;
            if (string.IsNullOrWhiteSpace(parsed.HeroTitleAccent)) parsed.HeroTitleAccent = def.HeroTitleAccent;

            return parsed;
        }
    }
}
