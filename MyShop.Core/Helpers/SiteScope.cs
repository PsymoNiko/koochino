using MyShop.Core.Interfaces;

namespace MyShop.Core.Helpers
{
    /// <summary>Helper for shared-DB multi-site scoping.</summary>
    public static class SiteScope
    {
        public static int? CurrentSiteId(ICurrentTenant tenant)
            => tenant != null && tenant.HasSite ? tenant.SiteId : (int?)null;

        public static bool ShouldFilter(ICurrentTenant tenant)
            => CurrentSiteId(tenant).HasValue;
    }
}
