using System;

namespace MyShop.Core.Routing
{
    /// <summary>نگاشت مسیرهای فروشگاه‌دار به کلید ماژول (همان کلیدهای StoreOwnerModules). اگر مسیر به ماژول وابسته نباشد، null برمی‌گرداند.</summary>
    public static class StoreOwnerRouteModuleMapping
    {
        /// <summary>برای مسیر داده‌شده (مثلاً /storeowner/products) نام ماژول را برمی‌گرداند؛ در غیر این صورت null.</summary>
        public static string GetModuleForPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return null;

            var normalized = path.Trim().ToLowerInvariant();
            var queryIndex = normalized.IndexOf('?');
            if (queryIndex >= 0)
                normalized = normalized.Substring(0, queryIndex);
            normalized = normalized.TrimEnd('/');

            if (!normalized.StartsWith("/storeowner"))
                return null;

            if (normalized.StartsWith("/storeowner/stores") || normalized.StartsWith("/storeowner/stores/print"))
                return "Stores";
            if (normalized.StartsWith("/storeowner/storedocuments"))
                return "Documents";
            if (normalized.StartsWith("/storeowner/products"))
                return "Products";
            if (normalized.StartsWith("/storeowner/orders"))
                return "Orders";
            if (normalized.StartsWith("/storeowner/gifts"))
                return "Coupons";
            if (normalized.StartsWith("/storeowner/chancebox"))
                return "ChanceBox";
            if (normalized.StartsWith("/storeowner/inventory"))
                return "Inventory";
            if (normalized.StartsWith("/storeowner/commerce"))
                return "Commerce";

            return null;
        }
    }
}
