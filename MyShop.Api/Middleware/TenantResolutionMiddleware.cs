using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyShop.Application.Interfaces.Sites;
using MyShop.Api.Services;

namespace MyShop.Api.Middleware
{
    public class TenantResolutionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IConfiguration _configuration;
        private readonly ILogger<TenantResolutionMiddleware> _logger;

        public TenantResolutionMiddleware(RequestDelegate next, IConfiguration configuration, ILogger<TenantResolutionMiddleware> logger)
        {
            _next = next;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var host = context.Request.Host.Value?.ToLowerInvariant().Trim() ?? "";

            var connectionString = ResolveConnectionString(host);
            context.Items[CurrentTenantService.TenantKeyItem] = host;
            context.Items[CurrentTenantService.ConnectionStringItem] = connectionString ?? string.Empty;

            int siteId = 0;
            string siteCode = "";
            string siteName = "";
            var resolutionErrored = false;

            try
            {
                var siteService = context.RequestServices.GetService<ISiteService>();
                if (siteService != null && !string.IsNullOrEmpty(host))
                {
                    var resolved = await siteService.ResolveByHostAsync(host);
                    if (resolved != null && resolved.SiteId > 0)
                    {
                        siteId = resolved.SiteId;
                        siteCode = resolved.Code ?? "";
                        siteName = resolved.Name ?? "";
                    }
                }
            }
            catch (Exception ex)
            {
                resolutionErrored = true;
                _logger.LogError(ex, "Tenant resolution failed for host {Host} via ResolveByHostAsync", host);
            }

            if (siteId <= 0)
            {
                var mapCode = _configuration[$"Sites:HostMap:{host}"]
                    ?? (host.StartsWith("www.") ? _configuration[$"Sites:HostMap:{host.Substring(4)}"] : null)
                    ?? _configuration["Sites:DefaultCode"]
                    ?? "koochino";
                try
                {
                    var siteService = context.RequestServices.GetService<ISiteService>();
                    var match = siteService != null ? await siteService.GetByCodeAsync(mapCode) : null;
                    if (match != null)
                    {
                        siteId = match.Id;
                        siteCode = match.Code;
                        siteName = match.Name;
                        resolutionErrored = false; // fallback succeeded — no longer an error condition
                    }
                    else
                    {
                        siteCode = mapCode;
                        siteName = mapCode;
                    }
                }
                catch (Exception ex)
                {
                    resolutionErrored = true;
                    _logger.LogError(ex, "Tenant resolution fallback failed for host {Host}, mapCode {MapCode}", host, mapCode);
                    siteCode = mapCode;
                    siteName = mapCode;
                }
            }

            // Fail closed: a real error (not just "host unmapped") must not silently
            // continue with SiteId=0, since that disables per-site row filtering
            // (SiteScope.ShouldFilter) across every tenant-scoped repository query.
            if (siteId <= 0 && resolutionErrored)
            {
                _logger.LogError("Tenant resolution unresolved for host {Host} after DB error — failing closed instead of continuing with SiteId=0", host);
                context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                await context.Response.WriteAsync("Service temporarily unavailable.");
                return;
            }

            context.Items[CurrentTenantService.SiteIdItem] = siteId;
            context.Items[CurrentTenantService.SiteCodeItem] = siteCode;
            context.Items[CurrentTenantService.SiteNameItem] = siteName;

            await _next(context);
        }

        string ResolveConnectionString(string host)
        {
            if (string.IsNullOrEmpty(host))
                return _configuration.GetConnectionString("Default");

            var connectionString = _configuration["ConnectionStrings:Tenants:" + host];

            if (string.IsNullOrWhiteSpace(connectionString) && host.StartsWith("www.", StringComparison.OrdinalIgnoreCase))
                connectionString = _configuration["ConnectionStrings:Tenants:" + host.Substring(4)];

            if (string.IsNullOrWhiteSpace(connectionString) && !host.StartsWith("www.", StringComparison.OrdinalIgnoreCase))
                connectionString = _configuration["ConnectionStrings:Tenants:www." + host];

            if (string.IsNullOrWhiteSpace(connectionString))
                connectionString = _configuration.GetConnectionString("Default");

            return connectionString;
        }
    }
}
