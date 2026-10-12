using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using MyShop.Application.Services.Commerce;
using MyShop.Core.Models.Commerce;

namespace MyShop.Tests
{
    public class CommerceFoundationTests
    {
        [Fact]
        public void ModuleKey_IsExistingCommerceKey()
        {
            Assert.Equal("Commerce", CommerceConstants.ModuleKey);
        }

        [Fact]
        public void OrderSource_DocumentsExistingIsCommerceSplit()
        {
            Assert.Equal("Legacy", CommerceConstants.OrderSource.Legacy);
            Assert.Equal("Commerce", CommerceConstants.OrderSource.Commerce);
        }

        [Fact]
        public void FeatureKeys_AreNamedForFutureUseOnly()
        {
            Assert.Equal("CatalogExtensions", CommerceConstants.Features.CatalogExtensions);
            Assert.Equal("Variations", CommerceConstants.Features.Variations);
            Assert.Equal("VariationCart", CommerceConstants.Features.VariationCart);
            Assert.Equal("Wishlist", CommerceConstants.Features.Wishlist);
            Assert.Equal("Promotions", CommerceConstants.Features.Promotions);
            Assert.Equal("Tax", CommerceConstants.Features.Tax);
            Assert.Equal("CouponsOnCheckout", CommerceConstants.Features.CouponsOnCheckout);
            Assert.Equal("Refunds", CommerceConstants.Features.Refunds);
            Assert.Equal("Reservation", CommerceConstants.Features.Reservation);
            Assert.Equal("MultiVendor", CommerceConstants.Features.MultiVendor);
            Assert.Equal("Reports", CommerceConstants.Features.Reports);
            Assert.Equal("Settlement", CommerceConstants.Features.Settlement);
        }

        [Fact]
        public void LogicalSubmodules_IncludeRefundsReservationsSettlement()
        {
            Assert.Equal("Refunds", CommerceConstants.Submodules.Refunds);
            Assert.Equal("Reservations", CommerceConstants.Submodules.Reservations);
            Assert.Equal("Settlement", CommerceConstants.Submodules.Settlement);
            Assert.Equal("Catalog", CommerceConstants.Submodules.Catalog);
        }

        [Fact]
        public void MissingFeatureConfig_IsOff()
        {
            var flags = new CommerceFeatureFlags(new MapConfig());
            Assert.False(flags.IsEnabled(CommerceConstants.Features.VariationCart));
            Assert.False(flags.IsEnabled(CommerceConstants.Features.Refunds));
            Assert.False(flags.IsEnabled(CommerceConstants.Features.Reservation));
            Assert.False(flags.IsEnabled(CommerceConstants.Features.MultiVendor));
            Assert.False(flags.IsEnabled(CommerceConstants.Features.Reports));
            Assert.False(flags.IsEnabled(CommerceConstants.Features.Settlement));
            Assert.False(flags.IsEnabled(""));
        }

        [Fact]
        public void FeatureFlag_TrueOnlyWhenConfigured()
        {
            var flags = new CommerceFeatureFlags(new MapConfig
            {
                ["Commerce:Features:VariationCart"] = "true"
            });
            Assert.True(flags.IsEnabled(CommerceConstants.Features.VariationCart));
            Assert.False(flags.IsEnabled(CommerceConstants.Features.MultiVendor));
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
            public IConfigurationSection GetSection(string key) => new Sec(key, ((IConfiguration)this)[key]);

            sealed class Noop : IChangeToken
            {
                public bool HasChanged => false;
                public bool ActiveChangeCallbacks => false;
                public IDisposable RegisterChangeCallback(Action<object?> callback, object? state) => new D();
                sealed class D : IDisposable { public void Dispose() { } }
            }

            sealed class Sec : IConfigurationSection
            {
                public Sec(string key, string? value) { Key = key; Path = key; Value = value; }
                public string Key { get; }
                public string Path { get; }
                public string? Value { get; set; }
                public string? this[string key] { get => null; set { } }
                public IEnumerable<IConfigurationSection> GetChildren() => Array.Empty<IConfigurationSection>();
                public IChangeToken GetReloadToken() => new Noop();
                public IConfigurationSection GetSection(string key) => new Sec(key, null);
            }
        }
    }
}
