using System;

namespace MyShop.Core.Routing
{
    /// <summary>نگاشت مسیرهای پورتال مشتری به کلید Feature برای موتور Permission.</summary>
    public static class CustomerRouteModuleMapping
    {
        public static string GetFeatureForPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return null;
            var p = path.Trim().ToLowerInvariant();
            var q = p.IndexOf('?');
            if (q >= 0) p = p.Substring(0, q);
            p = p.TrimEnd('/');
            if (!p.StartsWith("/customer")) return null;

            // Match only within the first path segment after the prefix, not anywhere in
            // the full remaining path -- an unanchored Contains() could otherwise match a
            // keyword that only appears in a deeper, unrelated segment (e.g. a nested
            // widget/sub-route name), resolving to the wrong feature/permission. Mirrors
            // StoreOwnerRouteModuleMapping's anchored StartsWith pattern.
            var rest = p.Length > "/customer".Length ? p.Substring("/customer".Length) : "";
            var seg = "/" + rest.TrimStart('/').Split('/')[0];

            if (seg.Contains("/dashboard")) return "Dashboard";
            if (seg.Contains("/orders") || seg.Contains("/submit-order") || seg.Contains("/commerce-orders")
                || seg.Contains("/order-success") || seg.Contains("/order-reviews") || seg.Contains("/dispute"))
                return "Orders";
            if (seg.Contains("/points") || seg.Contains("/missions")) return "Points";
            if (seg.Contains("/tickets")) return "Tickets";
            if (seg.Contains("/wallet")) return "Wallet";
            if (seg.Contains("/wishlist") || seg.Contains("/commerce")) return "Commerce";
            if (seg.Contains("/gifts") || seg.Contains("/coupon") || seg.Contains("/apply-coupon")) return "Coupons";
            if (seg.Contains("/lucky-league") || seg.Contains("/chancebox")) return "LuckyLeague";
            if (seg.Contains("/profile") || seg.Contains("/userprofile") || seg.Contains("/profileimages") || seg.Contains("/invite"))
                return "Profile";
            return null;
        }
    }
}
