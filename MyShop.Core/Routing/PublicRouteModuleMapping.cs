using System;

namespace MyShop.Core.Routing
{
    /// <summary>نگاشت مسیرهای عمومی (سایت/مشتری) به ماژول.</summary>
    public static class PublicRouteModuleMapping
    {
        /// <summary>برای مسیر داده‌شده (مثلاً /post/1 یا /stores) نام ماژول را برمی‌گرداند؛ وگرنه null.</summary>
        public static string GetModuleForPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return null;

            var normalized = path.Trim().ToLowerInvariant();
            var queryIndex = normalized.IndexOf('?');
            if (queryIndex >= 0)
                normalized = normalized.Substring(0, queryIndex);
            normalized = normalized.TrimEnd('/');

            if (normalized.StartsWith("/post") && (normalized == "/post" || normalized.StartsWith("/post/")))
                return "Blog";
            if (normalized == "/stores" || normalized.StartsWith("/stores?"))
                return "Stores";
            if (normalized.StartsWith("/store/"))
                return "Stores";
            if (normalized == "/products" || normalized.StartsWith("/products?") || normalized.StartsWith("/product/"))
                return "Products";
            if (normalized == "/shop" || normalized.StartsWith("/shop/") || normalized == "/cart" || normalized.StartsWith("/cart/") || normalized == "/checkout" || normalized.StartsWith("/checkout")
                || normalized.StartsWith("/commerce/") || normalized == "/customer/commerce-orders" || normalized.StartsWith("/customer/commerce-orders")
                || normalized == "/customer/wishlist" || normalized.StartsWith("/customer/wishlist"))
                return "Commerce";
            if (normalized == "/personnel" || normalized.StartsWith("/personnel?") || normalized.StartsWith("/personnel/"))
                return "Personnel";
            if (normalized.StartsWith("/customer/submit-order") || normalized.StartsWith("/customer/orders") || normalized.StartsWith("/customer/chancebox")
                || normalized.StartsWith("/customer/coupons") || normalized.StartsWith("/customer/gifts") || normalized.StartsWith("/customer/apply-coupon"))
                return "Shop";
            if (normalized.StartsWith("/storeowner/orders") || normalized.StartsWith("/storeowner/gifts") || normalized.StartsWith("/storeowner/chancebox")
                || normalized == "/storeowner/reports" || normalized.StartsWith("/storeowner/reports/"))
                return "Shop";
            if (normalized.StartsWith("/customer/lucky-league") || normalized.StartsWith("/storeowner/lucky-prizes"))
                return "LuckyLeague";
            if (normalized.StartsWith("/storeowner/commerce"))
                return "Commerce";
            if (normalized.StartsWith("/storeowner/products") || normalized.StartsWith("/storeowner/product"))
                return "Products";
            if (normalized.StartsWith("/storeowner/inventory") || normalized.StartsWith("/storeowner/stock") || normalized.StartsWith("/storeowner/warehouses"))
                return "Inventory";
            if (normalized.StartsWith("/storeowner/stores") || normalized.StartsWith("/storeowner/documents") || normalized.StartsWith("/storeowner/store-documents"))
                return "Stores";
            if (normalized == "/projects" || normalized.StartsWith("/projects?") || normalized.StartsWith("/project/"))
                return "ProjectManagement";
            if (normalized == "/customer/assistant" || normalized.StartsWith("/customer/assistant/"))
                return "AiAssistant";
            if (normalized == "/storeowner/assistant" || normalized.StartsWith("/storeowner/assistant/"))
                return "AiAssistant";
            if (normalized == "/storeowner/packages" || normalized.StartsWith("/storeowner/packages/"))
                return "VendorPackages";
            if (normalized == "/storeowner/billing" || normalized.StartsWith("/storeowner/billing")
                || normalized == "/storeowner/voucher-wallet" || normalized.StartsWith("/storeowner/voucher-wallet"))
                return "Finance";
            if (normalized == "/academy" || normalized.StartsWith("/academy/")
                || normalized == "/instructor" || normalized.StartsWith("/instructor/")
                || normalized == "/education" || normalized.StartsWith("/education/")
                || normalized == "/sport" || normalized.StartsWith("/sport/")
                || normalized == "/culture" || normalized.StartsWith("/culture/")
                || normalized == "/ceremony" || normalized.StartsWith("/ceremony/")
                || normalized == "/mission" || normalized.StartsWith("/mission/")
                || normalized == "/hall" || normalized.StartsWith("/hall/"))
                return "Academy";
            if (normalized == "/help" || normalized.StartsWith("/help/")
                || normalized == "/support" || normalized.StartsWith("/support/"))
                return "Support";
            if (normalized == "/services" || normalized.StartsWith("/services/")
                || normalized == "/charity" || normalized.StartsWith("/charity/"))
                return "Contact";
            if (normalized == "/library" || normalized.StartsWith("/library/"))
                return "Announcements";

            return null;
        }
    }
}
