using System;
using System.Collections.Generic;
using System.Linq;
using MyShop.Core.Models.Commerce;

namespace MyShop.Web.Services
{
    /// <summary>منبع واحد منوی ادمین — Layout فقط از اینجا رندر می‌کند.</summary>
    public static class AdminMenuConfig
    {
        public static readonly string[] DefaultModuleOrder = new[]
        {
            "Users",
            "Stores", "Shop", "Commerce", "Products", "Inventory", "Points", "VendorPackages",
            "AiAssistant", "Support", "Tickets", "Contact", "Sms", "Common", "SocialMedia",
            "Blog", "Sliders", "Announcements",
            "CRM", "Finance", "Contracts", "Assets", "Companies", "Personnel", "ProjectManagement", "Reports",
            "Academy", "Training"
        };

        public static IReadOnlyList<AdminMenuSection> GetSections()
        {
            return new List<AdminMenuSection>
            {
                new("عمومی", null, new List<AdminMenuGroup>
                {
                    new("home", "پیشخوان", "dashboard", null, new List<AdminMenuItem>
                    {
                        new("پیشخوان", "/admin/dashboard", IsRootLink: true)
                    })
                }),
                new("کاربران", "Users", new List<AdminMenuGroup>
                {
                    new("users", "کاربران و دسترسی", "users", "Users", new List<AdminMenuItem>
                    {
                        new("کاربران", "/admin/users"),
                        new("کاربران آنلاین", "/admin/online-users"),
                        new("نصب اپ (PWA)", "/admin/pwa-installs"),
                        new("سایت‌ها / برندها", "/admin/sites"),
                        new("بات‌های سایت", "/admin/site-bots"),
                        new("ایجاد ادمین", "/admin/create-admin", RequireAction: "Create"),
                        new("موتور دسترسی", "/admin/features/users/security-engine", RequireAction: "Read"),
                        new("نقش‌های امنیتی", "/admin/features/users/security-roles", RequireAction: "Update"),
                        new("Override کاربر", "/admin/features/users/security-overrides", RequireAction: "Update"),
                        new("شرکت‌های کاربر", "/admin/features/users/security-user-companies", RequireAction: "Update"),
                        new("گزارش دسترسی", "/admin/features/users/security-audit", RequireAction: "Read")
                    })
                }),
                new("فروش و فروشگاه", null, new List<AdminMenuGroup>
                {
                    new("stores", "فروشگاه", "shop", "Stores", new List<AdminMenuItem>
                    {
                        new("لیست فروشگاه‌ها", "/admin/stores"),
                        new("دسته‌بندی فروشگاه‌ها", "/admin/store-categories"),
                        new("سفارشات در انتظار تایید", "/admin/orders-pending")
                    }),
                    new("commerce", "فروشگاه آنلاین", "shop", "Commerce", new List<AdminMenuItem>
                    {
                        new("تنظیمات ارسال/پرداخت", "/admin/commerce/settings"),
                        new("سفارشات آنلاین", "/admin/commerce/orders"),
                        new("کاتالوگ تکمیلی", "/admin/commerce/catalog", FeatureFlag: CommerceConstants.Features.CatalogExtensions),
                        new("برندها", "/admin/commerce/brands", FeatureFlag: CommerceConstants.Features.CatalogExtensions),
                        new("برچسب‌ها", "/admin/commerce/tags", FeatureFlag: CommerceConstants.Features.CatalogExtensions),
                        new("دسته‌بندی محصول", "/admin/commerce/categories", FeatureFlag: CommerceConstants.Features.CatalogExtensions),
                        new("ویژگی‌ها", "/admin/commerce/attributes", FeatureFlag: CommerceConstants.Features.Variations),
                        new("پروموشن Commerce", "/admin/commerce/promotions", FeatureFlag: CommerceConstants.Features.Promotions),
                        new("استرداد", "/admin/commerce/orders", FeatureFlag: CommerceConstants.Features.Refunds),
                        new("گزارش فروش آنلاین", "/admin/commerce/reports", FeatureFlag: CommerceConstants.Features.Reports),
                        new("تسویه فروش آنلاین", "/admin/commerce/settlements", FeatureFlag: CommerceConstants.Features.Settlement)
                    }),
                    new("products", "محصولات", "tag", "Products", new List<AdminMenuItem>
                    {
                        new("لیست محصولات", "/admin/products"),
                        new("واحدها", "/admin/product-units"),
                        new("انواع قیمت", "/admin/prices")
                    }),
                    new("inventory", "انبار", "box", "Inventory", new List<AdminMenuItem>
                    {
                        new("انبارها", "/admin/inventory"),
                        new("موجودی", "/admin/inventory/stock"),
                        new("فاکتور خرید", "/admin/inventory/purchase"),
                        new("فاکتور فروش", "/admin/inventory/sale"),
                        new("تعدیل موجودی", "/admin/inventory/adjust")
                    }),
                    new("salesreports", "گزارشات فروش", "charts", "Shop", new List<AdminMenuItem>
                    {
                        new("گزارشات مالی و فروش", "/admin/sales-reports"),
                        new("سفارشات", "/admin/financial-orders")
                    }),
                    new("coupons", "کوپن", "price", "Shop", new List<AdminMenuItem>
                    {
                        new("مدیریت کوپن", "/admin/coupons")
                    }),
                    new("chancebox", "جعبه شانس", "kanban", "Shop", new List<AdminMenuItem>
                    {
                        new("فروشگاه‌های دارای جعبه‌شانس", "/admin/chancebox-stores")
                    }),
                    new("points", "امتیاز", "price", "Points", new List<AdminMenuItem>
                    {
                        new("امتیاز کاربران", "/admin/user-points"),
                        new("ماموریت‌ها", "/admin/point-missions"),
                        new("تراکنش‌ها", "/admin/point-transactions"),
                        new("پاداش خرید", "/admin/purchase-rewards"),
                        new("سگمنت / RFM", "/admin/segments"),
                        new("سطح VIP", "/admin/vip-tiers"),
                        new("کیف پول", "/admin/wallet"),
                        new("NPS", "/admin/nps"),
                        new("اتومیشن", "/admin/automations")
                    }),
                    new("luckyleague", "جعبه شانس لیگی", "kanban", "LuckyLeague", new List<AdminMenuItem>
                    {
                        new("لیگ‌ها و سطوح", "/admin/lucky-leagues"),
                        new("تنظیمات جعبه", "/admin/lucky-box-settings"),
                        new("جوایز", "/admin/lucky-prizes"),
                        new("گزارش باز کردن", "/admin/lucky-box-report")
                    }),
                    new("vendorpackages", "پکیج فروشنده", "shop", "VendorPackages", new List<AdminMenuItem>
                    {
                        new("مدیریت پکیج‌ها", "/admin/vendor-packages")
                    })
                }),
                new("هوش مصنوعی و ارتباط", null, new List<AdminMenuGroup>
                {
                    new("aiassistant", "دستیار هوشمند", "chat", "AiAssistant", new List<AdminMenuItem>
                    {
                        new("عیب‌یابی و تست", "/admin/ai/diagnostics"),
                        new("مصرف و هزینه", "/admin/ai/usage"),
                        new("سوالات بی‌پاسخ", "/admin/ai/unanswered"),
                        new("پایگاه دانش", "/admin/support/knowledge"),
                        new("دستیار مشتری", "/customer/assistant"),
                        new("پیشنهاد کمپین", "/admin/ai/suggestions")
                    }),
                    new("support", "پشتیبانی زنده", "chat", "Support", new List<AdminMenuItem>
                    {
                        new("گفتگوها", "/admin/support/conversations"),
                        new("گزارش‌ها", "/admin/support/reports"),
                        new("پاسخ‌های آماده", "/admin/support/canned"),
                        new("پایگاه دانش", "/admin/support/knowledge"),
                        new("تیم‌ها", "/admin/support/teams"),
                        new("مسدودها", "/admin/support/blocks"),
                        new("تنظیمات چت", "/admin/support/settings")
                    }),
                    new("tickets", "تیکت", "chat", "Tickets", new List<AdminMenuItem> { new("تیکت‌ها", "/admin/tickets") }),
                    new("contact", "ارتباط", "chat", "Contact", new List<AdminMenuItem> { new("درخواست تماس", "/admin/contact") }),
                    new("sms", "پیامک", "chat", "Sms", new List<AdminMenuItem>
                    {
                        new("تنظیمات پیامک", "/admin/sms"),
                        new("ارسال پیامک", "/admin/sms/send"),
                        new("گزارش ارسال", "/admin/sms/reports"),
                        new("قالب پیامک", "/admin/sms/templates"),
                        new("کمپین‌ها", "/admin/sms/campaigns")
                    }),
                    new("social", "شبکه اجتماعی", "chat", "Common", new List<AdminMenuItem>
                    {
                        new("کاربران", "/admin/social/users"),
                        new("منوی بات", "/admin/social/bot-menu"),
                        new("مدیریت کانال‌ها", "/admin/social/channels"),
                        new("ارسال به کانال‌ها", "/admin/social/channel-broadcast"),
                        new("لاگ پیام‌ها", "/admin/social/message-logs"),
                        new("بات بله", "/admin/bale"),
                        new("بازخورد بات", "/admin/bale-feedback"),
                        new("لینک پیام‌رسان", "/admin/messaging-links")
                    }),
                    new("socialmedia", "مدیریت شبکه‌های اجتماعی", "chat", "SocialMedia", new List<AdminMenuItem>
                    {
                        new("حساب‌های متصل", "/admin/social-media/accounts"),
                        new("Inbox یکپارچه", "/admin/social-media/inbox"),
                        new("انتشار / زمان‌بندی", "/admin/social-media/publish"),
                        new("لاگ عملیات", "/admin/social-media/logs")
                    })
                }),
                new("محتوا", null, new List<AdminMenuGroup>
                {
                    new("content", "محتوا", "pages", "Blog", new List<AdminMenuItem>
                    {
                        new("دست‌نوشته‌ها", "/admin/posts"),
                        new("نظرات", "/admin/comments")
                    }),
                    new("seo", "سئو", "pages", "Blog", new List<AdminMenuItem>
                    {
                        new("سئو صفحات", "/admin/seo")
                    }),
                    new("sliders", "اسلایدر", "pages", "Sliders", new List<AdminMenuItem>
                    {
                        new("اسلایدرها", "/admin/sliders")
                    }),
                    new("announcements", "اعلانات و صفحات", "chat", "Announcements", new List<AdminMenuItem>
                    {
                        new("اعلانات", "/admin/announcements"),
                        new("مدیریت صفحات", "/admin/site-pages"),
                        new("طرح چاپ فروشگاه", "/admin/print-designs"),
                        new("بخش‌های صفحه اول", "/admin/home-sections")
                    })
                }),
                new("سازمان", null, new List<AdminMenuGroup>
                {
                    new("crm", "CRM", "users", "CRM", new List<AdminMenuItem> { new("مخاطبین", "/admin/crm") }),
                    new("finance", "مالی", "price", "Finance", new List<AdminMenuItem>
                    {
                        new("حساب‌ها", "/admin/finance"),
                        new("صورتحساب مغازه‌داران", "/admin/finance/store-billing"),
                        new("تنظیمات مالی", "/admin/finance/store-billing/settings"),
                        new("انواع خدمت", "/admin/finance/store-billing/services"),
                        new("پکیج‌ها", "/admin/finance/store-billing/packages"),
                        new("قراردادها", "/admin/finance/store-billing/contracts"),
                        new("قوانین تأخیر", "/admin/finance/store-billing/overdue-rules"),
                        new("محاسبات کارمزد", "/admin/finance/store-billing/calculations"),
                        new("دوره‌های مالی", "/admin/finance/store-billing/periods"),
                        new("فاکتورها", "/admin/finance/store-billing/invoices"),
                        new("پرداخت‌ها", "/admin/finance/store-billing/payments"),
                        new("تخفیف و تعدیل", "/admin/finance/store-billing/discounts"),
                        new("گزارش مالی فروشگاه", "/admin/finance/store-billing/reports"),
                        new("کیف پول مغازه‌داران", "/admin/finance/store-billing/voucher-wallet")
                    }),
                    new("contracts", "قراردادها", "pages", "Contracts", new List<AdminMenuItem> { new("قراردادها", "/admin/contracts") }),
                    new("assets", "دارایی‌ها", "box", "Assets", new List<AdminMenuItem> { new("دارایی‌ها", "/admin/assets") }),
                    new("companies", "هلدینگ و شرکت", "layout", "Companies", new List<AdminMenuItem>
                    {
                        new("هلدینگ‌ها", "/admin/holdings"),
                        new("شرکت‌ها", "/admin/companies"),
                        new("گزارش به تفکیک شرکت", "/admin/companies-report")
                    }),
                    new("personnel", "پرسونل", "profile", "Personnel", new List<AdminMenuItem>
                    {
                        new("لیست پرسنل", "/admin/personnel")
                    }),
                    new("projects", "مدیریت پروژه", "projects", "ProjectManagement", new List<AdminMenuItem>
                    {
                        new("لیست پروژه‌ها", "/admin/projects"),
                        new("ایجاد پروژه", "/admin/projects/create", RequireAction: "Create"),
                        new("لاگ فعالیت‌ها", "/admin/projects/activity")
                    }),
                    new("reports", "گزارش‌ها", "charts", "Reports", new List<AdminMenuItem> { new("گزارش‌ها", "/admin/reports") })
                }),
                new("آموزش", null, new List<AdminMenuGroup>
                {
                    new("academy", "آموزشگاه", "pages", "Academy", new List<AdminMenuItem>
                    {
                        new("پیشخوان آموزشگاه", "/admin/academy"),
                        new("دانش‌آموزان", "/admin/academy/students"),
                        new("مربیان", "/admin/academy/instructors"),
                        new("مکان‌های برگزاری", "/admin/academy/locations"),
                        new("دسته‌بندی فعالیت‌ها", "/admin/academy/categories"),
                        new("فعالیت‌ها", "/admin/academy/activities"),
                        new("جلسات فعالیت", "/admin/academy/sessions"),
                        new("درخواست‌های فعالیت", "/admin/academy/requests"),
                        new("دوره‌ها", "/admin/academy/courses"),
                        new("درس و آزمون", "/admin/academy/lessons"),
                        new("ثبت‌نام دوره‌ها", "/admin/academy/enrollments"),
                        new("درخواست‌های روزانه", "/admin/academy/daily-requests"),
                        new("حضورغیاب", "/admin/academy/attendance"),
                        new("پرداخت شهریه", "/admin/academy/payments"),
                        new("اعلان‌ها", "/admin/academy/notifications"),
                        new("گزارش‌ها", "/admin/academy/reports"),
                        new("گزارش عملیاتی", "/admin/academy/reports/ops"),
                        new("گزارش روزانه", "/admin/academy/reports/daily"),
                        new("تقویم جلسات", "/admin/academy/calendar"),
                        new("گواهی پایان دوره", "/admin/academy/certificates"),
                        new("ارزیابی‌ها", "/admin/academy/evaluations"),
                        new("امتیاز آموزشگاه", "/admin/academy/points")
                    }),
                    new("training", "آموزش", "pages", "Training", new List<AdminMenuItem> { new("دوره‌ها", "/admin/training") })
                }),
                new("سیستم", null, new List<AdminMenuGroup>
                {
                    new("system", "سیستم", "settings", null, new List<AdminMenuItem>
                    {
                        new("مدیریت ماژول‌ها", "/admin/modules"),
                        new("تنظیمات", "/admin/settings"),
                        new("تنظیمات محرمانه / ماژول", "/admin/module-settings"),
                        new("استان و شهر", "/admin/locations")
                    })
                })
            };
        }

        /// <summary>سازگاری با AccessProfiles و ModuleSettings.</summary>
        public static IReadOnlyDictionary<string, List<AdminMenuItem>> GetItemsByModule()
        {
            var dict = new Dictionary<string, List<AdminMenuItem>>(StringComparer.OrdinalIgnoreCase);
            foreach (var section in GetSections())
            foreach (var g in section.Groups)
            {
                if (string.IsNullOrWhiteSpace(g.ModuleKey)) continue;
                if (!dict.TryGetValue(g.ModuleKey, out var list))
                {
                    list = new List<AdminMenuItem>();
                    dict[g.ModuleKey] = list;
                }
                foreach (var item in g.Items)
                    if (!list.Any(x => string.Equals(x.Href, item.Href, StringComparison.OrdinalIgnoreCase)))
                        list.Add(item);
            }
            return dict;
        }

        public static IReadOnlyList<AdminMenuItem> SystemItems { get; } = new List<AdminMenuItem>
        {
            new("مدیریت ماژول‌ها", "/admin/modules"),
            new("تنظیمات", "/admin/settings"),
            new("تنظیمات محرمانه / ماژول", "/admin/module-settings"),
            new("استان و شهر", "/admin/locations")
        };
    }

    public record AdminMenuSection(string Label, string ModuleKey, List<AdminMenuGroup> Groups);

    public record AdminMenuGroup(string Key, string Title, string Icon, string ModuleKey, List<AdminMenuItem> Items);

    public record AdminMenuItem(
        string Label,
        string Href,
        string RequireAction = null,
        bool IsRootLink = false,
        string FeatureFlag = null);
}
