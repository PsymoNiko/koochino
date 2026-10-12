using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using MyShop.Application.Interfaces.Settings;
using System;
using System.Linq;

namespace MyShop.Web.Services
{
    public class PlatformContextService : IPlatformContextService
    {
        private readonly IHttpContextAccessor _http;
        private readonly IConfiguration _config;

        public PlatformContextService(IHttpContextAccessor http, IConfiguration config)
        {
            _http = http;
            _config = config;
        }

        public string CurrentHost
        {
            get
            {
                var host = _http.HttpContext?.Request?.Host.Host ?? "";
                return host.Trim().ToLowerInvariant();
            }
        }

        public bool IsSuperAdminHost()
        {
            var hosts = _config.GetSection("Platform:SuperAdminHosts").Get<string[]>() ?? Array.Empty<string>();
            if (hosts.Length == 0)
            {
                hosts = new[] { "koochino.shop", "www.koochino.shop" };
            }
            var h = CurrentHost;
            return hosts.Any(x => string.Equals(x.Trim(), h, StringComparison.OrdinalIgnoreCase));
        }

        public bool CanManageAllSites(int adminUserId, bool isFullAccess) =>
            adminUserId > 0 && isFullAccess && IsSuperAdminHost();
    }
}
