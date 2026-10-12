using Microsoft.AspNetCore.Builder;
using MyShop.Web.Middleware;

namespace MyShop.Web.Extensions
{
    public static class AdminModuleGuardMiddlewareExtensions
    {
        /// <summary>استفاده از میدلور محافظت روت ادمین بر اساس وضعیت ماژول‌ها.</summary>
        public static IApplicationBuilder UseAdminModuleGuard(this IApplicationBuilder app)
        {
            return app.UseMiddleware<AdminModuleGuardMiddleware>();
        }

        /// <summary>استفاده از میدلور محافظت روت‌های عمومی (سایت/مشتری) بر اساس وضعیت ماژول‌ها.</summary>
        public static IApplicationBuilder UsePublicModuleGuard(this IApplicationBuilder app)
        {
            return app.UseMiddleware<PublicModuleGuardMiddleware>();
        }

        /// <summary>استفاده از میدلور محافظت روت‌های فروشگاه‌دار بر اساس وضعیت ماژول‌ها و مجوز دسترسی.</summary>
        public static IApplicationBuilder UseStoreOwnerModuleGuard(this IApplicationBuilder app)
        {
            return app.UseMiddleware<StoreOwnerModuleGuardMiddleware>();
        }
    }
}
