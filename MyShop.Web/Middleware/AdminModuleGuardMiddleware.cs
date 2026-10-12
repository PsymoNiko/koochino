using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MyShop.Core.Routing;
using System;
using System.Threading.Tasks;

namespace MyShop.Web.Middleware
{
    /// <summary>مسیرهای ادمین: فعال‌بودن ماژول در سایت/تنظیمات (بدون استنتاج مجوز از URL).</summary>
    public class AdminModuleGuardMiddleware
    {
        private readonly RequestDelegate _next;

        public AdminModuleGuardMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value ?? "";
            if (!path.StartsWith("/admin", StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }

            var module = AdminRouteModuleMapping.GetModuleForPath(path);
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
                var moduleSettings = context.RequestServices.GetService<MyShop.Web.Services.IModuleSettingsService>();
                if (moduleSettings != null)
                {
                    var flags = moduleSettings.GetModules();
                    if (flags != null && flags.TryGetValue(module, out var on))
                        moduleOn = on;
                }
                else
                {
                    var config = context.RequestServices.GetService<IConfiguration>();
                    if (config != null)
                        moduleOn = config.GetValue($"Modules:{module}", true);
                }
            }

            if (!moduleOn)
            {
                context.Response.Redirect("/admin/dashboard?moduleDisabled=" + Uri.EscapeDataString(module), false);
                return;
            }

            await _next(context);
        }
    }
}
