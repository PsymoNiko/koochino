using Microsoft.AspNetCore.Http;
using MyShop.Core.Interfaces;

namespace MyShop.Api.Services
{
    public class CurrentTenantService : ICurrentTenant
    {
        public const string TenantKeyItem = "TenantKey";
        public const string ConnectionStringItem = "TenantConnectionString";
        public const string SiteIdItem = "SiteId";
        public const string SiteCodeItem = "SiteCode";
        public const string SiteNameItem = "SiteName";

        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentTenantService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string TenantKey =>
            _httpContextAccessor.HttpContext?.Items[TenantKeyItem] as string ?? string.Empty;

        public string ConnectionString =>
            _httpContextAccessor.HttpContext?.Items[ConnectionStringItem] as string ?? string.Empty;

        public int SiteId
        {
            get
            {
                var ctx = _httpContextAccessor.HttpContext;
                if (ctx?.Items[SiteIdItem] is int id) return id;
                if (ctx?.Items[SiteIdItem] is string s && int.TryParse(s, out var parsed)) return parsed;
                return 0;
            }
        }

        public string SiteCode =>
            _httpContextAccessor.HttpContext?.Items[SiteCodeItem] as string ?? string.Empty;

        public string SiteName =>
            _httpContextAccessor.HttpContext?.Items[SiteNameItem] as string ?? string.Empty;

        public bool IsResolved =>
            !string.IsNullOrWhiteSpace(ConnectionString);

        public bool HasSite => SiteId > 0;
    }
}
