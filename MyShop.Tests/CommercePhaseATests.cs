using MyShop.Application.Interfaces;
using MyShop.Application.Interfaces.Commerce;
using MyShop.Application.Interfaces.Products;
using MyShop.Application.Services.Commerce;
using MyShop.Core.Models;
using MyShop.Core.Models.Commerce;
using MyShop.Core.Models.Products;

namespace MyShop.Tests
{
    public class CommercePhaseATests
    {
        [Fact]
        public void Seller_SeesOrder_ByHeaderStoreId_WhenLineMissing()
        {
            var order = new CommerceOrderDetail
            {
                StoreId = 7,
                Items = new List<OrderItem> { new() { ProductId = 1 } }
            };
            Assert.True(CommerceService.SellerCanSee(order, new HashSet<int> { 7 }));
            Assert.False(CommerceService.SellerCanSee(order, new HashSet<int> { 99 }));
        }

        [Fact]
        public void Seller_SeesOrder_ByLineStoreId_EvenIfHeaderDiffers()
        {
            var order = new CommerceOrderDetail
            {
                StoreId = 1,
                Items = new List<OrderItem> { new() { ProductId = 1, StoreId = 22 } }
            };
            Assert.True(CommerceService.SellerCanSee(order, new HashSet<int> { 22 }));
            Assert.False(CommerceService.SellerCanSee(order, new HashSet<int> { 1 }));
        }

        [Fact]
        public void HeaderNull_NotSellerVisible()
        {
            var order = new CommerceOrderDetail { StoreId = null, Items = new List<OrderItem>() };
            Assert.False(CommerceService.SellerCanSee(order, new HashSet<int> { 7 }));
        }

        [Fact]
        public async Task OtherSeller_CannotFulfill()
        {
            var orders = new MemSellerOrders
            {
                Detail = new CommerceOrderDetail { Id = 5, StoreId = 7, Items = new List<OrderItem>() }
            };
            var svc = new CommerceService(null, null, orders, Array.Empty<IPaymentGateway>(), stores: new OneStore(3, 7));
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                svc.SetSellerFulfillmentAsync(99, 5, CommerceFulfillmentStatus.Processing, null, null, null));
            Assert.False(orders.Updated);
        }

        [Fact]
        public async Task Owner_CanFulfillOwnHeaderStore()
        {
            var orders = new MemSellerOrders
            {
                Detail = new CommerceOrderDetail { Id = 5, StoreId = 7, Items = new List<OrderItem>() }
            };
            var svc = new CommerceService(null, null, orders, Array.Empty<IPaymentGateway>(), stores: new OneStore(3, 7));
            await svc.SetSellerFulfillmentAsync(3, 5, CommerceFulfillmentStatus.Processing, null, null, null);
            Assert.True(orders.Updated);
        }

        [Fact]
        public async Task Shop_HidesOtherSiteProducts()
        {
            var shop = new CommerceShopCatalogService(new TwoProducts(), new TwoSiteStores());
            var visible = await shop.ListVisibleAsync(10);
            Assert.Single(visible);
            Assert.Equal(1, visible[0].Id);
        }

        [Fact]
        public async Task Shop_WithoutSite_ShowsActive()
        {
            var shop = new CommerceShopCatalogService(new TwoProducts(), new TwoSiteStores());
            var visible = await shop.ListVisibleAsync(null);
            Assert.Equal(2, visible.Count);
        }

        [Fact]
        public async Task AddToCart_IgnoresClientPriceAndTitle()
        {
            var carts = new MemCart();
            var products = new PricedProduct();
            var svc = new CommerceService(null, carts, null, Array.Empty<IPaymentGateway>(), products, pricing: new CommercePricingService());
            var cart = await svc.AddToCartAsync(null, 1, "g", 1, "hacked-title", 1, 1);
            Assert.Equal("Real", cart.Items[0].Title);
            Assert.Equal(5000, cart.Items[0].UnitPrice);
        }

        sealed class MemSellerOrders : ICommerceOrderRepository
        {
            public CommerceOrderDetail Detail = new();
            public bool Updated;
            public Task<int> CreateCommerceOrderAsync(CommerceOrderDetail order, IEnumerable<OrderItem> items, int? changedByUserId) => Task.FromResult(0);
            public Task<CommerceOrderDetail> GetDetailAsync(int orderId) => Task.FromResult(Detail);
            public Task<List<CommerceOrderDetail>> ListAsync(int? siteId = null, int? userId = null, int? storeId = null, int top = 200)
                => Task.FromResult(new List<CommerceOrderDetail>());
            public Task<List<CommerceOrderDetail>> ListForStoreIdsAsync(IReadOnlyList<int> storeIds, int top = 200)
                => Task.FromResult(new List<CommerceOrderDetail> { Detail }.Where(o => storeIds.Contains(o.StoreId ?? 0)).ToList());
            public Task UpdateFulfillmentAsync(int orderId, byte status, string trackingCode, string note, int? changedByUserId)
            {
                Updated = true;
                return Task.CompletedTask;
            }
            public Task UpdatePaymentStatusAsync(int orderId, byte paymentStatus, byte? fulfillmentStatus = null) => Task.CompletedTask;
            public Task<List<OrderStatusHistory>> ListHistoryAsync(int orderId) => Task.FromResult(new List<OrderStatusHistory>());
            public Task<int> InsertPaymentAsync(CommercePayment payment) => Task.FromResult(1);
            public Task UpdatePaymentAsync(CommercePayment payment) => Task.CompletedTask;
            public Task<CommercePayment> GetPaymentByAuthorityAsync(string authority) => Task.FromResult<CommercePayment>(null!);
            public Task<CommercePayment> GetPaymentByIdAsync(int paymentId) => Task.FromResult<CommercePayment>(null!);
            public Task<bool> TryMarkPaymentPaidAsync(int paymentId, string refId, string rawResponse) => Task.FromResult(false);
            public Task<bool> TryBeginPaymentFinalizationAsync(int paymentId) => Task.FromResult(false);
            public Task MarkPaymentFinalizationDoneAsync(int paymentId) => Task.CompletedTask;
            public Task AbandonPaymentFinalizationAsync(int paymentId) => Task.CompletedTask;
            public Task<bool> TryMarkPaymentFailedIfNotPaidAsync(int paymentId, string rawResponse) => Task.FromResult(false);
            public Task InsertPaymentAttemptAsync(CommercePaymentAttempt attempt) => Task.CompletedTask;
            public Task<CommerceDashboardKpis> GetDashboardKpisAsync(int? siteId) => Task.FromResult(new CommerceDashboardKpis());
            public Task<bool> OrderItemColumnExistsAsync(string columnName) => Task.FromResult(false);
            public Task<bool> OrderColumnExistsAsync(string columnName) => Task.FromResult(false);
        }

        sealed class OneStore : IStoreService
        {
            readonly int _userId, _storeId;
            public OneStore(int userId, int storeId) { _userId = userId; _storeId = storeId; }
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

        sealed class TwoProducts : IProductService
        {
            public Task<ProductDto> GetByIdAsync(int id) => Task.FromResult<ProductDto>(null!);
            public Task<List<ProductDto>> GetAllAsync(int? storeId = null, int? companyId = null) =>
                Task.FromResult(new List<ProductDto>
                {
                    new() { Id = 1, Name = "A", IsActive = true, StoreId = 10 },
                    new() { Id = 2, Name = "B", IsActive = true, StoreId = 20 }
                });
            public Task<int> CreateAsync(CreateProductDto dto) => Task.FromResult(0);
            public Task<bool> UpdateAsync(UpdateProductDto dto) => Task.FromResult(true);
            public Task<bool> DeleteAsync(int id) => Task.FromResult(true);
        }

        sealed class TwoSiteStores : IStoreService
        {
            public Task<List<Store>> GetByUserIdAsync(int userId) => Task.FromResult(new List<Store>());
            public Task<int> CountAsync() => Task.FromResult(0);
            public Task<List<Store>> LatestAsync(int count) => Task.FromResult(new List<Store>());
            public Task<List<Store>> TopSellingAsync(int count) => Task.FromResult(new List<Store>());
            public Task<Store> GetByIdAsync(int storeId) => Task.FromResult<Store>(null!);
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

        sealed class PricedProduct : IProductService
        {
            public Task<ProductDto> GetByIdAsync(int id) =>
                Task.FromResult(new ProductDto { Id = 1, Name = "Real", IsActive = true, Price = 5000, Quantity = 9 });
            public Task<List<ProductDto>> GetAllAsync(int? storeId = null, int? companyId = null) => Task.FromResult(new List<ProductDto>());
            public Task<int> CreateAsync(CreateProductDto dto) => Task.FromResult(0);
            public Task<bool> UpdateAsync(UpdateProductDto dto) => Task.FromResult(true);
            public Task<bool> DeleteAsync(int id) => Task.FromResult(true);
        }

        sealed class MemCart : ICommerceCartRepository
        {
            public CommerceCart Cart = new() { Id = 1, Items = new List<CommerceCartItem>() };
            public Task<CommerceCart> GetOrCreateAsync(int? siteId, int? userId, string guestKey) => Task.FromResult(Cart);
            public Task<CommerceCart> GetWithItemsAsync(int cartId) => Task.FromResult(Cart);
            public Task SetItemAsync(int cartId, int productId, string title, long unitPrice, int quantity)
            {
                Cart.Items.Clear();
                Cart.Items.Add(new CommerceCartItem { CartId = cartId, ProductId = productId, Title = title, UnitPrice = unitPrice, Quantity = quantity });
                return Task.CompletedTask;
            }
            public Task RemoveItemAsync(int cartId, int productId) => Task.CompletedTask;
            public Task ClearAsync(int cartId) => Task.CompletedTask;
            public Task AttachUserAsync(int cartId, int userId) => Task.CompletedTask;
        }
    }
}
