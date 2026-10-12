using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using MyShop.Application.Services.Commerce;
using MyShop.Core.Models.Commerce;
using MyShop.Core.Routing;

namespace MyShop.Tests
{
    public class CommercePhaseLTests
    {
        [Theory]
        [InlineData("/shop")]
        [InlineData("/shop/product/1")]
        [InlineData("/cart")]
        [InlineData("/checkout")]
        [InlineData("/customer/commerce-orders")]
        public void LiveStorefrontRoutes_StillMapToCommerce(string path)
        {
            Assert.Equal("Commerce", PublicRouteModuleMapping.GetModuleForPath(path));
        }

        [Theory]
        [InlineData("/storeowner/billing")]
        [InlineData("/storeowner/voucher-wallet")]
        public void StoreBillingRoute_StillFinance(string path)
        {
            Assert.Equal("Finance", PublicRouteModuleMapping.GetModuleForPath(path));
        }

        [Fact]
        public void ModuleKey_Unchanged()
        {
            Assert.Equal("Commerce", CommerceConstants.ModuleKey);
        }

        [Fact]
        public void EveryFeatureFlag_DefaultsOff()
        {
            var flags = new CommerceFeatureFlags(new MapConfig());
            var names = typeof(CommerceConstants.Features)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(f => f.IsLiteral && f.FieldType == typeof(string) && f.Name != nameof(CommerceConstants.Features.ConfigSection))
                .Select(f => (string)f.GetRawConstantValue()!);
            foreach (var name in names)
                Assert.False(flags.IsEnabled(name), name);
        }

        [Fact]
        public void SettlementService_IsNotStoreBilling()
        {
            var ifaces = typeof(CommerceSettlementService).GetInterfaces().Select(i => i.Name).ToHashSet();
            Assert.Contains(nameof(MyShop.Application.Interfaces.Commerce.ICommerceSettlementService), ifaces);
            Assert.DoesNotContain("IStoreBillingCommissionService", ifaces);
            Assert.DoesNotContain("IStoreBillingRepository", ifaces);
        }

        [Fact]
        public void NewAdminPaths_MapToCommerce()
        {
            Assert.Equal("Commerce", AdminRouteModuleMapping.GetModuleForPath("/admin/commerce/settlements"));
            Assert.Equal("Commerce", AdminRouteModuleMapping.GetModuleForPath("/admin/commerce/reports"));
            Assert.Equal("Commerce", AdminRouteModuleMapping.GetModuleForPath("/admin/commerce/orders"));
        }

        sealed class MapConfig : Dictionary<string, string>, IConfiguration
        {
            public MapConfig() : base(StringComparer.OrdinalIgnoreCase) { }
            string? IConfiguration.this[string key]
            {
                get => TryGetValue(key ?? "", out var v) ? v : null;
                set { if (key != null && value != null) this[key] = value; }
            }
            public IEnumerable<IConfigurationSection> GetChildren() => Array.Empty<IConfigurationSection>();
            public IChangeToken GetReloadToken() => new Noop();
            public IConfigurationSection GetSection(string key) => new Dummy();
            sealed class Noop : IChangeToken
            {
                public bool ActiveChangeCallbacks => false;
                public bool HasChanged => false;
                public IDisposable RegisterChangeCallback(Action<object?> callback, object? state) => new D();
            }
            sealed class D : IDisposable { public void Dispose() { } }
            sealed class Dummy : IConfigurationSection
            {
                public string Key => "";
                public string Path => "";
                public string? Value { get; set; }
                public string? this[string key] { get => null; set { } }
                public IEnumerable<IConfigurationSection> GetChildren() => Array.Empty<IConfigurationSection>();
                public IChangeToken GetReloadToken() => new Noop();
                public IConfigurationSection GetSection(string key) => this;
            }
        }
    }
}
