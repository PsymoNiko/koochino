using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using MyShop.Application.Interfaces.Modules;
using MyShop.Core.Interfaces;

namespace MyShop.Api.Services
{
    public class LicenseCallerContext : ILicenseCallerContext
    {
        private readonly IHttpContextAccessor _http;
        private readonly ICurrentTenant _tenant;

        public LicenseCallerContext(IHttpContextAccessor http, ICurrentTenant tenant)
        {
            _http = http;
            _tenant = tenant;
        }

        ClaimsPrincipal User => _http.HttpContext?.User;

        public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;

        public int? UserId
        {
            get
            {
                var raw = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? User?.FindFirst("UserId")?.Value;
                return int.TryParse(raw, out var id) && id > 0 ? id : null;
            }
        }

        public bool IsAdmin => User?.IsInRole("Admin") == true;

        public bool IsStoreOwner => User?.IsInRole("StoreOwner") == true;

        public int? TenantSiteId => _tenant != null && _tenant.HasSite ? _tenant.SiteId : null;
    }
}
