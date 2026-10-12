using MyShop.Application.Interfaces.Commerce;
using MyShop.Application.Services.Commerce;
using MyShop.Core.Models.Commerce;
using MyShop.Core.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace MyShop.Tests
{
    public class CommerceCatalogPhase2Tests
    {
        InMemoryCommerceCatalog Seed()
        {
            var d = new InMemoryCommerceCatalog();
            d.AccessibleProducts.Add(1);
            d.AccessibleProducts.Add(2);
            d.ProductSites[1] = 10;
            d.ProductSites[2] = 10;
            d.ProductSkus[1] = "PARENT";
            return d;
        }

        ICommerceVariationService Vars(InMemoryCommerceCatalog d) =>
            new CommerceVariationService(new MemVars(d), new MemAccess(d), new MemAttrs(d));

        [Fact]
        public async Task Variation_Crud_And_DuplicateSku()
        {
            var d = Seed();
            var svc = Vars(d);
            var id = await svc.SaveAsync(new CommerceProductVariation { ProductId = 1, Sku = "RED-M", Price = 1000, Stock = 3, IsActive = true }, 10);
            Assert.True(id > 0);
            var list = await svc.ListAsync(1, 10);
            Assert.Single(list);
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                svc.SaveAsync(new CommerceProductVariation { ProductId = 1, Sku = "RED-M", IsActive = true }, 10));
        }

        [Fact]
        public async Task Variation_SiteIsolation_Idor()
        {
            var d = Seed();
            var svc = Vars(d);
            await svc.SaveAsync(new CommerceProductVariation { ProductId = 1, Sku = "X1", IsActive = true }, 10);
            await Assert.ThrowsAsync<InvalidOperationException>(() => svc.ListAsync(1, 99));
        }

        [Fact]
        public async Task Variation_Rejects_UnrelatedTerm()
        {
            var d = Seed();
            var attrs = new CommerceAttributeService(new MemAttrs(d));
            var colorId = await attrs.SaveAttributeAsync(new CommerceAttribute { Name = "Color", SiteId = 10, IsActive = true });
            var sizeId = await attrs.SaveAttributeAsync(new CommerceAttribute { Name = "Size", SiteId = 10, IsActive = true });
            var red = await attrs.SaveTermAsync(new CommerceAttributeTerm { AttributeId = colorId, Name = "Red", IsActive = true }, 10);
            var svc = Vars(d);
            var vid = await svc.SaveAsync(new CommerceProductVariation { ProductId = 1, Sku = "V1", IsActive = true }, 10);
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                svc.SetAttributesAsync(vid, new[] { new CommerceVariationAttribute { AttributeId = sizeId, TermId = red } }, 10));
        }

        [Fact]
        public async Task Variation_SetDefault_OnlyOnePerProduct()
        {
            var d = Seed();
            var svc = Vars(d);
            var a = await svc.SaveAsync(new CommerceProductVariation { ProductId = 1, Sku = "A", IsActive = true }, 10);
            var b = await svc.SaveAsync(new CommerceProductVariation { ProductId = 1, Sku = "B", IsActive = true }, 10);
            await svc.SetDefaultAsync(a, 10);
            await svc.SetDefaultAsync(b, 10);
            var list = await svc.ListAsync(1, 10);
            Assert.Single(list.Where(v => v.IsDefault));
            Assert.Equal(b, list.First(v => v.IsDefault).Id);
            var def = await svc.GetDefaultAsync(1, 10);
            Assert.Equal(b, def.Id);
        }

        [Fact]
        public async Task Gallery_Crud_ValidatesUrl()
        {
            var d = Seed();
            var svc = new CommerceProductImageService(new MemImages(d), new MemAccess(d));
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                svc.SaveAsync(new CommerceProductImage { ProductId = 1, Url = "javascript:alert(1)" }, 10));
            var id = await svc.SaveAsync(new CommerceProductImage { ProductId = 1, Url = "/uploads/a.jpg", IsActive = true }, 10);
            Assert.True(id > 0);
            Assert.Single(await svc.ListAsync(1, 10));
        }

        [Fact]
        public async Task Relations_RejectSelf_And_Duplicate()
        {
            var d = Seed();
            var svc = new CommerceProductRelationService(new MemRels(d), new MemAccess(d));
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                svc.SaveAsync(new CommerceProductRelation { ProductId = 1, RelatedProductId = 1, RelationType = "Related" }, 10));
            await svc.SaveAsync(new CommerceProductRelation { ProductId = 1, RelatedProductId = 2, RelationType = "Upsell" }, 10);
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                svc.SaveAsync(new CommerceProductRelation { ProductId = 1, RelatedProductId = 2, RelationType = "Upsell" }, 10));
        }

        [Fact]
        public void FeatureFlags_DefaultOff()
        {
            var flags = new CommerceFeatureFlags(new IndexConfig());
            Assert.False(flags.IsEnabled(CommerceConstants.Features.Variations));
            Assert.False(flags.IsEnabled(CommerceConstants.Features.CatalogExtensions));
        }

        [Fact]
        public void FeatureFlags_On_WhenConfigured()
        {
            var flags = new CommerceFeatureFlags(new IndexConfig
            {
                ["Commerce:Features:Variations"] = "true"
            });
            Assert.True(flags.IsEnabled(CommerceConstants.Features.Variations));
            Assert.False(flags.IsEnabled(CommerceConstants.Features.CatalogExtensions));
        }

        [Fact]
        public void PublicShopCartCheckout_StillMapToCommerce()
        {
            Assert.Equal("Commerce", PublicRouteModuleMapping.GetModuleForPath("/shop"));
            Assert.Equal("Commerce", PublicRouteModuleMapping.GetModuleForPath("/cart"));
            Assert.Equal("Commerce", PublicRouteModuleMapping.GetModuleForPath("/checkout"));
            Assert.Equal("Commerce", PublicRouteModuleMapping.GetModuleForPath("/commerce/payment/callback"));
        }

        [Fact]
        public void AdminCatalogRoutes_UseExistingModules()
        {
            Assert.Equal("Commerce", AdminRouteModuleMapping.GetModuleForPath("/admin/commerce/brands"));
            Assert.Equal("Commerce", AdminRouteModuleMapping.GetModuleForPath("/admin/commerce/catalog"));
            Assert.Equal("Products", AdminRouteModuleMapping.GetModuleForPath("/admin/products/5/variations"));
            Assert.Equal("Products", AdminRouteModuleMapping.GetModuleForPath("/admin/products/5/gallery"));
        }

        [Fact]
        public async Task Brand_Assign_IdorOtherSiteProduct()
        {
            var d = Seed();
            d.ProductSites[3] = 11;
            d.AccessibleProducts.Add(3);
            var brands = new CommerceBrandService(new MemBrands(d), new MemAccess(d));
            var bid = await brands.SaveAsync(new CommerceBrand { Name = "Acme", SiteId = 10, IsActive = true });
            await Assert.ThrowsAsync<InvalidOperationException>(() => brands.AssignToProductAsync(3, bid, 10));
        }

        sealed class IndexConfig : IConfiguration
        {
            readonly Dictionary<string, string> _map = new(StringComparer.OrdinalIgnoreCase);
            public string this[string key]
            {
                get => _map.TryGetValue(key ?? "", out var v) ? v : null;
                set { if (key != null) _map[key] = value; }
            }
            public IEnumerable<IConfigurationSection> GetChildren() => Array.Empty<IConfigurationSection>();
            public IChangeToken GetReloadToken() => new NoopToken();
            public IConfigurationSection GetSection(string key) => new DummySection(key, this[key]);

            sealed class NoopToken : IChangeToken
            {
                public bool HasChanged => false;
                public bool ActiveChangeCallbacks => false;
                public IDisposable RegisterChangeCallback(Action<object> callback, object state) => new D();
                sealed class D : IDisposable { public void Dispose() { } }
            }

            sealed class DummySection : IConfigurationSection
            {
                public DummySection(string key, string value) { Key = key; Value = value; Path = key; }
                public string Key { get; }
                public string Path { get; }
                public string Value { get; set; }
                public string this[string key] { get => null; set { } }
                public IEnumerable<IConfigurationSection> GetChildren() => Array.Empty<IConfigurationSection>();
                public IChangeToken GetReloadToken() => new NoopToken();
                public IConfigurationSection GetSection(string key) => new DummySection(key, null);
            }
        }
    }
}
