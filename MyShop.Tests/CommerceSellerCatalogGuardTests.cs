using MyShop.Application.Interfaces;
using MyShop.Application.Interfaces.Products;
using MyShop.Application.Services.Commerce;
using MyShop.Core.Models;
using MyShop.Core.Models.Products;
using MyShop.Core.Routing;

namespace MyShop.Tests
{
    public class CommerceSellerCatalogGuardTests
    {
        [Fact]
        public async Task Owner_CanAccessOwnProduct()
        {
            var g = new CommerceSellerCatalogGuard(new P(7), new S(3, 7));
            await g.EnsureOwnsProductAsync(3, 10);
        }

        [Fact]
        public async Task OtherSeller_IsRejected()
        {
            var g = new CommerceSellerCatalogGuard(new P(7), new S(3, 99));
            await Assert.ThrowsAsync<InvalidOperationException>(() => g.EnsureOwnsProductAsync(3, 10));
        }

        [Fact]
        public void StoreOwnerCatalogRoute_MapsToCommerce()
        {
            Assert.Equal("Commerce", PublicRouteModuleMapping.GetModuleForPath("/storeowner/commerce-catalog"));
            Assert.Equal("Products", PublicRouteModuleMapping.GetModuleForPath("/storeowner/products/10/catalog/gallery"));
        }

        sealed class P : IProductService
        {
            readonly int _storeId;
            public P(int storeId) => _storeId = storeId;
            public Task<ProductDto> GetByIdAsync(int id) => Task.FromResult(new ProductDto { Id = id, StoreId = _storeId, Name = "x" });
            public Task<List<ProductDto>> GetAllAsync(int? storeId = null, int? companyId = null) => Task.FromResult(new List<ProductDto>());
            public Task<int> CreateAsync(CreateProductDto dto) => Task.FromResult(0);
            public Task<bool> UpdateAsync(UpdateProductDto dto) => Task.FromResult(true);
            public Task<bool> DeleteAsync(int id) => Task.FromResult(true);
        }

        sealed class S : IStoreService
        {
            readonly int _userId, _storeId;
            public S(int userId, int storeId) { _userId = userId; _storeId = storeId; }
            public Task<List<Store>> GetByUserIdAsync(int userId)
                => Task.FromResult(userId == _userId ? new List<Store> { new() { StoreID = _storeId } } : new List<Store>());
            public Task<int> CountAsync() => Task.FromResult(0);
            public Task<List<Store>> LatestAsync(int count) => Task.FromResult(new List<Store>());
            public Task<List<Store>> TopSellingAsync(int count) => Task.FromResult(new List<Store>());
            public Task<Store> GetByIdAsync(int storeId) => Task.FromResult<Store>(null!);
            public Task<int> CreateAsync(Store store) => Task.FromResult(0);
            public Task<bool> UpdateAsync(Store store) => Task.FromResult(true);
            public Task<(bool CanDelete, List<string> Reasons)> CanDeleteAsync(int storeId) => Task.FromResult((true, new List<string>()));
            public Task<bool> DeleteAsync(int storeId) => Task.FromResult(true);
            public Task<bool> ToggleStatusAsync(int storeId, bool status) => Task.FromResult(true);
            public Task<List<Store>> GetAllAsync() => Task.FromResult(new List<Store>());
            public Task<List<Store>> GetAllForUserAsync(int userId, int? siteId = null) => GetAllAsync();
            public Task<List<Store>> GetListForPublicAsync(string search, bool onlyWithCoupon, string sortBy, int? categoryId = null, int? cityId = null)
                => Task.FromResult(new List<Store>());
        }
    }
}
