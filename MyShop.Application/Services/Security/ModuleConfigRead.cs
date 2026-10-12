using System.Threading.Tasks;
using MyShop.Application.Interfaces.Security;
using MyShop.Core.Interfaces;

namespace MyShop.Application.Services.Security
{
    public static class ModuleConfigRead
    {
        public static int? SiteIdOf(ICurrentTenant tenant)
            => tenant != null && tenant.HasSite ? tenant.SiteId : (int?)null;

        public static async Task<string> TryGetAsync(
            IModuleConfigService config,
            int? siteId,
            string moduleKey,
            string settingKey,
            bool secret,
            bool allowLegacyFallback = true)
        {
            if (config == null) return null;
            try
            {
                var v = secret
                    ? await config.GetSecretAsync(siteId, moduleKey, settingKey, allowLegacyFallback)
                    : await config.GetStringAsync(siteId, moduleKey, settingKey, allowLegacyFallback);
                return string.IsNullOrWhiteSpace(v) ? null : v.Trim();
            }
            catch
            {
                return null;
            }
        }
    }
}
