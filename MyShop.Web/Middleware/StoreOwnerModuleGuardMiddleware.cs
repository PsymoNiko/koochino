using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MyShop.Application.Interfaces.Inventory;
using MyShop.Core.Routing;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MyShop.Web.Middleware
{
    /// <summary>مسیرهای فروشگاه‌دار: ماژول سراسری روشن + مجوز دسترسی فروشگاه‌دار به ماژول.</summary>
    public class StoreOwnerModuleGuardMiddleware
    {
        private readonly RequestDelegate _next;

        public StoreOwnerModuleGuardMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value ?? "";
            if (!path.StartsWith("/storeowner", StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }

            var module = StoreOwnerRouteModuleMapping.GetModuleForPath(path);
            if (module == null)
            {
                await _next(context);
                return;
            }

            var moduleOn = true;
            var tenantEarly = context.RequestServices.GetService<MyShop.Core.Interfaces.ICurrentTenant>();
            var siteModules = context.RequestServices.GetService<MyShop.Application.Interfaces.Modules.ISiteModuleService>();
            if (siteModules != null && tenantEarly != null && tenantEarly.HasSite)
            {
                moduleOn = await siteModules.IsEnabledAsync(tenantEarly.SiteId, module, globalFallback: true);
            }
            else
            {
                var config = context.RequestServices.GetService<IConfiguration>();
                if (config != null)
                    moduleOn = config.GetValue($"Modules:{module}", true);
            }

            if (!moduleOn)
            {
                context.Response.Redirect("/storeowner/dashboard?moduleDisabled=" + Uri.EscapeDataString(module), false);
                return;
            }

            var user = context.User;
            // فقط برای فروشگاه‌دار احراز هویت‌شده مجوز ماژول را اعمال کن
            if (user?.Identity?.IsAuthenticated != true || !user.IsInRole("StoreOwner"))
            {
                await _next(context);
                return;
            }

            try
            {
                var idStr = user.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(idStr, out var userId) || userId <= 0)
                {
                    context.Response.Redirect("/storeowner/dashboard?accessDenied=" + Uri.EscapeDataString(module), false);
                    return;
                }

                var perms = context.RequestServices.GetService<IStoreOwnerPermissionService>();
                if (perms == null)
                {
                    await _next(context);
                    return;
                }

                var allowed = await perms.HasAsync(userId, module);
                if (!allowed)
                {
                    context.Response.Redirect("/storeowner/dashboard?accessDenied=" + Uri.EscapeDataString(module), false);
                    return;
                }
            }
            catch
            {
                // fail-closed: در خطای SQL دسترسی را رد کن
                context.Response.Redirect("/storeowner/dashboard?accessDenied=" + Uri.EscapeDataString(module), false);
                return;
            }

            await _next(context);
        }
    }
}
