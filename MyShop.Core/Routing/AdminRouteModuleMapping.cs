using System;

namespace MyShop.Core.Routing
{
    /// <summary>نگاشت مسیرهای ادمین به ماژول. اگر مسیر به ماژول وابسته نباشد، null برمی‌گرداند.</summary>
    public static class AdminRouteModuleMapping
    {
        private static readonly string[] AllowedPaths = new[]
        {
            "/admin",
            "/admin/",
            "/admin/dashboard",
            "/admin/index",
            "/admin/modules",
            "/admin/settings",
            "/admin/profile"
        };

        /// <summary>برای مسیر داده‌شده (مثلاً /admin/posts) نام ماژول را برمی‌گرداند؛ در غیر این صورت null.</summary>
        public static string GetModuleForPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return null;

            var normalized = path.Trim().ToLowerInvariant();
            var queryIndex = normalized.IndexOf('?');
            if (queryIndex >= 0)
                normalized = normalized.Substring(0, queryIndex);
            normalized = normalized.TrimEnd('/');
            if (normalized == "")
                normalized = "/";

            // Exact match only — prefix match on "/admin" would incorrectly clear every admin route.
            foreach (var allowed in AllowedPaths)
            {
                var a = allowed.TrimEnd('/').ToLowerInvariant();
                if (normalized == a)
                    return null;
            }

            if (normalized.StartsWith("/admin/users") || normalized.StartsWith("/admin/features/users") || normalized.StartsWith("/admin/online-users") || normalized.StartsWith("/admin/create-admin") || normalized.StartsWith("/admin/pwa-installs") || normalized.StartsWith("/admin/access-profiles") || normalized.StartsWith("/admin/access-audit") || normalized.StartsWith("/admin/sites") || normalized.StartsWith("/admin/site-bots"))
                return "Users";
            if (normalized.StartsWith("/admin/stores") || normalized.StartsWith("/admin/store-documents") || normalized.StartsWith("/admin/orders-pending"))
                return "Stores";
            if (normalized.StartsWith("/admin/sales-reports") || normalized.StartsWith("/admin/financial-orders") || normalized.StartsWith("/admin/coupons") || normalized.StartsWith("/admin/chancebox-stores"))
                return "Shop";
            if (normalized.StartsWith("/admin/commerce"))
                return "Commerce";
            if (normalized.StartsWith("/admin/point-missions") || normalized.StartsWith("/admin/point-transactions") || normalized.StartsWith("/admin/user-points")
                || normalized.StartsWith("/admin/segments") || normalized.StartsWith("/admin/vip-tiers") || normalized.StartsWith("/admin/wallet")
                || normalized.StartsWith("/admin/nps") || normalized.StartsWith("/admin/automations")
                || normalized.StartsWith("/admin/purchase-rewards"))
                return "Points";
            if (normalized.StartsWith("/admin/lucky-leagues") || normalized.StartsWith("/admin/lucky-prizes")
                || normalized.StartsWith("/admin/lucky-box-report") || normalized.StartsWith("/admin/lucky-box-settings"))
                return "LuckyLeague";
            if (normalized.StartsWith("/admin/sliders"))
                return "Sliders";
            if (normalized.StartsWith("/admin/posts") || normalized.StartsWith("/admin/comments") || normalized.StartsWith("/admin/seo"))
                return "Blog";
            if (normalized.StartsWith("/admin/store-categories"))
                return "Stores";
            if (normalized.StartsWith("/admin/social/") || normalized.StartsWith("/admin/bale-feedback"))
                return "Common";
            if (normalized.StartsWith("/admin/announcements") || normalized.StartsWith("/admin/home-sections") || normalized.StartsWith("/admin/site-pages")
                || normalized.StartsWith("/admin/print-designs"))
                return "Announcements";
            if (normalized.StartsWith("/admin/social-media"))
                return "SocialMedia";
            if (normalized.StartsWith("/admin/bale") || normalized.StartsWith("/admin/messaging-links") || normalized.StartsWith("/admin/social"))
                return "Common";
            if (normalized.StartsWith("/admin/sms"))
                return "Sms";
            if (normalized.StartsWith("/admin/vendor-packages"))
                return "VendorPackages";
            if (normalized.StartsWith("/admin/support"))
                return "Support";
            if (normalized.StartsWith("/admin/ai"))
                return "AiAssistant";
            if (normalized.StartsWith("/admin/products") || normalized.StartsWith("/admin/product-units") || normalized.StartsWith("/admin/prices"))
                return "Products";
            if (normalized.StartsWith("/admin/personnel"))
                return "Personnel";
            if (normalized.StartsWith("/admin/projects"))
                return "ProjectManagement";
            if (normalized.StartsWith("/admin/inventory"))
                return "Inventory";
            if (normalized.StartsWith("/admin/crm"))
                return "CRM";
            if (normalized.StartsWith("/admin/tickets"))
                return "Tickets";
            if (normalized.StartsWith("/admin/contact"))
                return "Contact";
            if (normalized.StartsWith("/admin/reports"))
                return "Reports";
            if (normalized.StartsWith("/admin/finance"))
                return "Finance";
            if (normalized.StartsWith("/admin/contracts"))
                return "Contracts";
            if (normalized.StartsWith("/admin/assets"))
                return "Assets";
            if (normalized.StartsWith("/admin/training"))
                return "Training";
            if (normalized.StartsWith("/admin/academy"))
                return "Academy";
            if (normalized.StartsWith("/admin/holdings") || normalized.StartsWith("/admin/companies") || normalized.StartsWith("/admin/companies-report"))
                return "Companies";

            return null;
        }
    }
}
