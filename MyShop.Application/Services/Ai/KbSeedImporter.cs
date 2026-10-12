using MyShop.Application.Interfaces.Ai;
using MyShop.Application.Interfaces.SupportChat;
using MyShop.Core.Models.Ai;
using MyShop.Core.Models.SupportChat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MyShop.Application.Services.Ai
{
    /// <summary>
    /// Turns <see cref="KochinoKnowledgeBase"/> into published knowledge-base articles. Retrieval can only
    /// answer from the database, so an empty knowledge base makes the assistant useless; this gives it a
    /// starting corpus. Re-running updates the same rows through SourceKey instead of duplicating them.
    /// </summary>
    public class KbSeedImporter : IKbSeedImporter
    {
        const string SourcePrefix = "faq:";
        static readonly Regex CamelBoundary = new(@"(?<!^)(?=[A-Z])", RegexOptions.Compiled);

        /// <summary>
        /// Explicit Persian titles. The FAQ records only carry match keywords, and keywords like "فایده"
        /// make terrible article titles for a public help page.
        /// </summary>
        static readonly Dictionary<string, string> Titles = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Intro"] = "کوچینو چیست؟",
            ["CustomerBenefit"] = "کوچینو چه فایده‌ای برای مشتری دارد؟",
            ["Register"] = "ثبت‌نام در کوچینو",
            ["Login"] = "ورود به حساب مشتری",
            ["SelectStore"] = "انتخاب فروشگاه برای ثبت خرید",
            ["RegisterPurchase"] = "ثبت خرید حضوری",
            ["ChanceCode"] = "کد شانس چیست؟",
            ["Discount"] = "کد تخفیف چیست؟",
            ["UseDiscount"] = "کد تخفیف را کجا استفاده کنم؟",
            ["ConvertDiscount"] = "تبدیل تخفیف به امتیاز",
            ["Points"] = "امتیاز چیست؟",
            ["EarnPoints"] = "چگونه امتیاز بگیرم؟",
            ["UsePoints"] = "امتیازها را چگونه استفاده کنم؟",
            ["Mission"] = "مأموریت چیست؟",
            ["DoMission"] = "چگونه مأموریت انجام دهم؟",
            ["Invite"] = "دعوت از دوستان",
            ["Review"] = "ثبت نظر و امتیاز به فروشگاه",
            ["StorePhoto"] = "ارسال عکس فروشگاه",
            ["Events"] = "رویدادها و قرعه‌کشی",
            ["Prize"] = "جایزه چگونه محاسبه می‌شود؟",
            ["PurchaseProblem"] = "خرید، جایزه یا امتیاز ثبت نشده است",
            ["StoreBenefit"] = "کوچینو چه فایده‌ای برای مغازه‌دار دارد؟",
            ["StoreRegister"] = "ثبت فروشگاه در کوچینو",
            ["StoreLogin"] = "ورود به پنل کسب‌وکار",
            ["DefinePrize"] = "تعریف جایزه برای مشتریان",
            ["DefineChance"] = "تعریف کد شانس",
            ["DefineDiscount"] = "تعریف کد تخفیف",
            ["LoyaltyStore"] = "وفادارسازی مشتری",
            ["StoreMission"] = "تعریف مأموریت برای مشتری",
            ["AttractCustomer"] = "جذب مشتری جدید",
            ["EncouragePurchase"] = "تشویق مشتری به ثبت خرید",
            ["WhyKochino"] = "چرا کوچینو؟ تفاوت با سیستم تخفیف ساده",
            ["StoreFlow"] = "مسیر شروع کار مغازه‌دار",
            ["StoreCampaign"] = "کمپین و پیامک",
            ["StoreProfile"] = "ویرایش اطلاعات فروشگاه",
            ["StoreSupport"] = "مشکل در پنل مغازه‌دار",
            ["StoreOwnerIntro"] = "معرفی کوچینو برای مغازه‌دار"
        };

        private readonly IKnowledgeService _kb;

        public KbSeedImporter(IKnowledgeService kb) => _kb = kb;

        public async Task<KbSeedResult> ImportAsync(int? siteId, int? authorUserId = null)
        {
            int created = 0, updated = 0, skipped = 0;
            try
            {
                var categories = await EnsureCategoriesAsync(siteId);

                foreach (var faq in KochinoKnowledgeBase.All)
                {
                    if (faq == null || string.IsNullOrWhiteSpace(faq.Answer)) { skipped++; continue; }

                    var sourceKey = SourcePrefix + faq.Intent;
                    var existing = await _kb.GetBySourceKeyAsync(siteId, sourceKey);

                    var article = existing ?? new KnowledgeArticle();
                    article.SiteId = siteId is > 0 ? siteId : null;
                    article.SourceKey = sourceKey;
                    article.Title = ResolveTitle(faq);
                    if (string.IsNullOrWhiteSpace(article.Slug))
                        article.Slug = BuildSlug(faq.Intent);
                    article.Content = faq.Answer.Trim();
                    article.Summary = PersianTextScorer.Excerpt(null, faq.Answer, 300);
                    article.ArticleType = KnowledgeArticleTypes.Faq;
                    article.Audience = MapAudience(faq.Audience);
                    article.Status = KnowledgeArticleStatuses.Published;
                    article.IsPublic = true;
                    article.CategoryId = categories.TryGetValue(article.Audience, out var catId) ? catId : null;

                    await _kb.SaveArticleAsync(article, BuildTags(faq), authorUserId);
                    if (existing == null) created++; else updated++;
                }

                return new KbSeedResult(created, updated, skipped);
            }
            catch (Exception ex)
            {
                return new KbSeedResult(created, updated, skipped, ex.Message);
            }
        }

        async Task<Dictionary<string, int>> EnsureCategoriesAsync(int? siteId)
        {
            var wanted = new (string Audience, string Name)[]
            {
                (KnowledgeAudiences.Both, "راهنمای عمومی"),
                (KnowledgeAudiences.Customer, "راهنمای مشتری"),
                (KnowledgeAudiences.StoreOwner, "راهنمای مغازه‌دار")
            };

            var existing = await _kb.ListCategoriesAsync(siteId);
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (var (audience, name) in wanted)
            {
                var match = existing?.FirstOrDefault(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));
                if (match != null) { map[audience] = match.Id; continue; }

                var id = await _kb.SaveCategoryAsync(new KnowledgeCategory
                {
                    Name = name,
                    SiteId = siteId is > 0 ? siteId : null,
                    IsActive = true
                });
                map[audience] = id;
            }

            return map;
        }

        static string ResolveTitle(KnowledgeFaq faq)
        {
            if (Titles.TryGetValue(faq.Intent ?? "", out var title)) return title;
            var firstKeyword = faq.Keywords?.FirstOrDefault(k => !string.IsNullOrWhiteSpace(k));
            return string.IsNullOrWhiteSpace(firstKeyword) ? faq.Intent : firstKeyword.Trim();
        }

        static IEnumerable<string> BuildTags(KnowledgeFaq faq) =>
            (faq.Keywords ?? Array.Empty<string>())
                .Select(k => (k ?? "").Trim())
                .Where(k => k.Length >= 2)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(10);

        static string BuildSlug(string intent)
        {
            if (string.IsNullOrWhiteSpace(intent)) return "faq-" + Guid.NewGuid().ToString("N").Substring(0, 8);
            return string.Join("-", CamelBoundary.Split(intent)).ToLowerInvariant();
        }

        static string MapAudience(AiAudience audience) => audience switch
        {
            AiAudience.Customer => KnowledgeAudiences.Customer,
            AiAudience.StoreOwner => KnowledgeAudiences.StoreOwner,
            _ => KnowledgeAudiences.Both
        };
    }
}
