using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MyShop.Application.Interfaces.Modules;
using MyShop.Core.Routing;
using System;
using System.Threading.Tasks;

namespace MyShop.Web.Middleware
{
    /// <summary>درخواست‌های GET به مسیرهای عمومی وابسته به ماژول؛ اولویت با entitlement سایت جاری.</summary>
    public class PublicModuleGuardMiddleware
    {
        private readonly RequestDelegate _next;

        public PublicModuleGuardMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (!string.Equals(context.Request.Method, "GET", StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }

            var path = context.Request.Path.Value ?? "";
            var module = PublicRouteModuleMapping.GetModuleForPath(path);
            if (module == null)
            {
                await _next(context);
                return;
            }

            var moduleOn = true;
            var tenant = context.RequestServices.GetService<MyShop.Core.Interfaces.ICurrentTenant>();
            var siteModules = context.RequestServices.GetService<ISiteModuleService>();
            if (siteModules != null && tenant != null && tenant.HasSite)
            {
                moduleOn = await siteModules.IsEnabledAsync(tenant.SiteId, module, globalFallback: true);
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
                context.Response.Redirect("/ModuleDisabled?module=" + Uri.EscapeDataString(module), false);
                return;
            }

            await _next(context);
        }
    }
}
