using MyShop.Application.Interfaces;
using MyShop.Application.Interfaces.Commerce;
using MyShop.Application.Interfaces.Products;
using MyShop.Application.Services.Commerce;
using MyShop.Core.Models;
using MyShop.Core.Models.Commerce;
using MyShop.Core.Models.Products;
using MyShop.Core.Routing;

namespace MyShop.Tests
{
    public class CommercePhaseBTests
    {
        [Fact]
        public void ProductDetailRoute_MapsToCommerce()
        {
            Assert.Equal("Commerce", PublicRouteModuleMapping.GetModuleForPath("/shop/product/12"));
            Assert.Equal("Commerce", PublicRouteModuleMapping.GetModuleForPath("/shop"));
        }

        [Fact]
        public async Task VisibleProduct_SameSite_Returned()
        {
            var svc = new CommerceShopCatalogService(new CatalogProducts(), new CatalogStores());
            var p = await svc.GetVisibleAsync(1, 10);
            Assert.NotNull(p);
            Assert.Equal(1, p.Id);
        }

        [Fact]
        public async Task OtherSiteProduct_NotFound()
        {
            var svc = new CommerceShopCatalogService(new CatalogProducts(), new CatalogStores());
            Assert.Null(await svc.GetVisibleAsync(2, 10));
        }

        [Fact]
        public async Task InactiveOrUnassigned_NotFound()
        {
            var svc = new CommerceShopCatalogService(new CatalogProducts(), new CatalogStores());
            Assert.Null(await svc.GetVisibleAsync(3, 10));
            Assert.Null(await svc.GetVisibleAsync(4, 10));
            Assert.Null(await svc.GetVisibleAsync(99, 10));
        }

        [Fact]
        public void VariationPicker_RequiresBothFlags()
        {
            var off = new FlagMap();
            Assert.False(CommerceShopCatalogService.VariationPickerEnabled(off));
            off.Map[CommerceConstants.Features.Variations] = true;
            Assert.False(CommerceShopCatalogService.VariationPickerEnabled(off));
            off.Map[CommerceConstants.Features.VariationCart] = true;
            Assert.True(CommerceShopCatalogService.VariationPickerEnabled(off));
        }

        sealed class FlagMap : ICommerceFeatureFlags
        {
            public Dictionary<string, bool> Map = new(StringComparer.OrdinalIgnoreCase);
            public bool IsEnabled(string featureKey) => Map.TryGetValue(featureKey, out var v) && v;
        }

        sealed class CatalogProducts : IProductService
        {
            readonly List<ProductDto> _all = new()
            {
                new() { Id = 1, Name = "Mine", IsActive = true, StoreId = 10, Price = 1000 },
                new() { Id = 2, Name = "OtherSite", IsActive = true, StoreId = 20, Price = 1000 },
                new() { Id = 3, Name = "Off", IsActive = false, StoreId = 10, Price = 1000 },
                new() { Id = 4, Name = "NoStore", IsActive = true, StoreId = null, Price = 1000 }
            };
            public Task<ProductDto> GetByIdAsync(int id) => Task.FromResult(_all.FirstOrDefault(p => p.Id == id));
            public Task<List<ProductDto>> GetAllAsync(int? storeId = null, int? companyId = null) => Task.FromResult(_all);
            public Task<int> CreateAsync(CreateProductDto dto) => Task.FromResult(0);
            public Task<bool> UpdateAsync(UpdateProductDto dto) => Task.FromResult(true);
            public Task<bool> DeleteAsync(int id) => Task.FromResult(true);
        }

        sealed class CatalogStores : IStoreService
        {
            public Task<List<Store>> GetByUserIdAsync(int userId) => Task.FromResult(new List<Store>());
            public Task<int> CountAsync() => Task.FromResult(0);
            public Task<List<Store>> LatestAsync(int count) => Task.FromResult(new List<Store>());
            public Task<List<Store>> TopSellingAsync(int count) => Task.FromResult(new List<Store>());
            public Task<Store> GetByIdAsync(int storeId) =>
                Task.FromResult(storeId switch
                {
                    10 => new Store { StoreID = 10, SiteId = 10 },
                    20 => new Store { StoreID = 20, SiteId = 99 },
                    _ => null!
                });
            public Task<int> CreateAsync(Store store) => Task.FromResult(0);
            public Task<bool> UpdateAsync(Store store) => Task.FromResult(true);
            public Task<(bool CanDelete, List<string> Reasons)> CanDeleteAsync(int storeId) => Task.FromResult((true, new List<string>()));
            public Task<bool> DeleteAsync(int storeId) => Task.FromResult(true);
            public Task<bool> ToggleStatusAsync(int storeId, bool status) => Task.FromResult(true);
            public Task<List<Store>> GetAllAsync() => Task.FromResult(new List<Store>
            {
                new() { StoreID = 10, SiteId = 10 },
                new() { StoreID = 20, SiteId = 99 }
            });
            public Task<List<Store>> GetAllForUserAsync(int userId, int? siteId = null) => GetAllAsync();
            public Task<List<Store>> GetListForPublicAsync(string search, bool onlyWithCoupon, string sortBy, int? categoryId = null, int? cityId = null)
                => Task.FromResult(new List<Store>());
        }
    }
}
