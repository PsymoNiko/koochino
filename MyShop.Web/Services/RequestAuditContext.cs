using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using MyShop.Application.Interfaces.Security;
using MyShop.Core.Interfaces;

namespace MyShop.Web.Services
{
    public class RequestAuditContext : IRequestAuditContext
    {
        readonly IHttpContextAccessor _http;
        readonly ICurrentTenant _tenant;

        public RequestAuditContext(IHttpContextAccessor http, ICurrentTenant tenant)
        {
            _http = http;
            _tenant = tenant;
        }

        ClaimsPrincipal User => _http.HttpContext?.User;

        public int? UserId
        {
            get
            {
                var raw = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User?.FindFirst("UserId")?.Value;
                return int.TryParse(raw, out var id) && id > 0 ? id : null;
            }
        }

        // Sync property — must not block on async permission engine (Blazor Server deadlock).
        public bool IsAdmin => User?.IsInRole("Admin") == true;

        public string ActorRole => User?.FindFirst(ClaimTypes.Role)?.Value;
        public int? TenantSiteId => _tenant?.HasSite == true ? _tenant.SiteId : null;
        public string ClientIp => _http.HttpContext?.Connection?.RemoteIpAddress?.ToString();
        public string UserAgent
        {
            get
            {
                var ua = _http.HttpContext?.Request?.Headers.UserAgent.ToString();
                return string.IsNullOrEmpty(ua) ? null : (ua.Length > 500 ? ua.Substring(0, 500) : ua);
            }
        }
    }
}
