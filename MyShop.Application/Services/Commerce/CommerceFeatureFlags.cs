using Microsoft.Extensions.Configuration;
using MyShop.Application.Interfaces.Commerce;
using MyShop.Core.Models.Commerce;

namespace MyShop.Application.Services.Commerce
{
    public class CommerceFeatureFlags : ICommerceFeatureFlags
    {
        private readonly IConfiguration _configuration;

        public CommerceFeatureFlags(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public bool IsEnabled(string featureKey)
        {
            if (string.IsNullOrWhiteSpace(featureKey) || _configuration == null)
                return false;

            var name = featureKey.Trim();
            var prefix = CommerceConstants.Features.ConfigSection + ":";
            if (name.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase))
                name = name.Substring(prefix.Length);

            var raw = _configuration[prefix + name];
            if (string.IsNullOrWhiteSpace(raw))
                return false;
            if (bool.TryParse(raw, out var b))
                return b;
            return raw == "1" || string.Equals(raw, "yes", System.StringComparison.OrdinalIgnoreCase);
        }
    }
}
