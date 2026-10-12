using MyShop.Application.Interfaces.Commerce;
using MyShop.Application.Interfaces.Products;
using MyShop.Application.Services.Commerce;
using MyShop.Core.Models.Commerce;
using MyShop.Core.Models.Products;

namespace MyShop.Tests
{
    public class CommercePhaseCTests
    {
        [Fact]
        public void MixedStores_Rejected()
        {
            var r = CommerceService.SingleStoreFromLines(new[]
            {
                new OrderItem { StoreId = 1, ProductId = 1 },
                new OrderItem { StoreId = 2, ProductId = 2 }
            });
            Assert.False(r.Ok);
        }

        [Fact]
        public void SameStoreTwoLines_Accepted()
        {
            var r = CommerceService.SingleStoreFromLines(new[]
            {
                new OrderItem { StoreId = 7, ProductId = 1 },
                new OrderItem { StoreId = 7, ProductId = 2, VariationId = 11 }
            });
            Assert.True(r.Ok);
            Assert.Equal(7, r.StoreId);
        }

        [Fact]
        public async Task FlagOff_EmptySimpleCart_Unchanged()
        {
            var h = Harness.Off();
            var r = await h.Svc.CheckoutAsync(1, 9, "g", Req(), "https://x");
            Assert.False(r.Success);
            Assert.Contains("خالی", r.Message);
            Assert.Empty(h.Orders.Created);
        }

        [Fact]
        public async Task FlagOn_VariationOnly_CreatesLinesWithStoreAndVariation()
        {
            var h = Harness.On();
            h.VarCart.Items.Add(new CommerceVariationCartItem
            {
                CartId = 1, ProductId = 1, VariationId = 11, Quantity = 2, UnitPriceSnapshot = 1
            });
            var r = await h.Svc.CheckoutAsync(1, 9, "g", Req(), "https://x");
            Assert.True(r.Success);
            Assert.Single(h.Orders.Created);
            var line = h.Orders.Created[0].Items[0];
            Assert.Equal(11, line.VariationId);
            Assert.Equal(7, line.StoreId);
            Assert.Equal(7, h.Orders.Created[0].Order.StoreId);
            Assert.Equal(3, h.Vars.All[0].Stock);
            Assert.Equal(10, h.Products.Qty);
            Assert.Empty(h.VarCart.Items);
        }

        [Fact]
        public async Task FlagOn_SimpleLine_DecrementsProductNotVariation()
        {
            var h = Harness.On();
            h.Carts.Cart.Items.Add(new CommerceCartItem { ProductId = 1, Title = "P", Quantity = 2, UnitPrice = 50 });
            var r = await h.Svc.CheckoutAsync(1, 9, "g", Req(), "https://x");
            Assert.True(r.Success);
            Assert.Null(h.Orders.Created[0].Items[0].VariationId);
            Assert.Equal(8, h.Products.Qty);
            Assert.Equal(5, h.Vars.All[0].Stock);
        }

        [Fact]
        public async Task FlagOn_MixedStoreProducts_Fails()
        {
            var h = Harness.On();
            h.Products.Extra = new ProductDto { Id = 2, Name = "B", IsActive = true, Price = 10, Quantity = 9, StoreId = 99 };
            h.Carts.Cart.Items.Add(new CommerceCartItem { ProductId = 1, Title = "A", Quantity = 1, UnitPrice = 50 });
            h.Carts.Cart.Items.Add(new CommerceCartItem { ProductId = 2, Title = "B", Quantity = 1, UnitPrice = 50 });
            var r = await h.Svc.CheckoutAsync(1, 9, "g", Req(), "https://x");
            Assert.False(r.Success);
            Assert.Contains("یک فروشگاه", r.Message);
        }

        static CommerceCheckoutRequest Req() => new()
        {
            ShippingMethodId = 1,
            PaymentMethodId = 1,
            ShippingAddress = "تهران"
        };

        sealed class Harness
        {
            public CommerceService Svc = null!;
            public MemCatalog Catalog = new();
            public MemCart Carts = new();
            public MemCOrders Orders = new();
            public MemP Products = new();
            public MemVars Vars = new();
            public MemVarCart VarCart = new();
            public Flag Flags = new();

            public static Harness Off() => Create(false);
            public static Harness On() => Create(true);

            static Harness Create(bool on)
            {
                var h = new Harness();
                h.Flags.On = on;
                h.Svc = new CommerceService(
                    h.Catalog, h.Carts, h.Orders, Array.Empty<IPaymentGateway>(),
                    h.Products, pricing: new CommercePricingService(),
                    flags: h.Flags, varCart: h.VarCart, variations: h.Vars);
                return h;
            }
        }

        sealed class Flag : ICommerceFeatureFlags
        {
            public bool On;
            public bool IsEnabled(string featureKey) =>
                On && string.Equals(featureKey, CommerceConstants.Features.VariationCart, StringComparison.OrdinalIgnoreCase);
        }

        sealed class MemCatalog : ICommerceCatalogRepository
        {
            public Task<List<CommerceShippingMethod>> ListShippingAsync(int? siteId, bool activeOnly = false) => Task.FromResult(new List<CommerceShippingMethod>());
            public Task<CommerceShippingMethod> GetShippingAsync(int id) => Task.FromResult(new CommerceShippingMethod { Id = 1, IsActive = true, Fee = 0 });
            public Task<int> UpsertShippingAsync(CommerceShippingMethod m) => Task.FromResult(1);
            public Task DeleteShippingAsync(int id) => Task.CompletedTask;
            public Task<List<CommercePaymentMethod>> ListPaymentAsync(int? siteId, bool activeOnly = false) => Task.FromResult(new List<CommercePaymentMethod>());
            public Task<CommercePaymentMethod> GetPaymentAsync(int id) =>
                Task.FromResult(new CommercePaymentMethod { Id = 1, IsActive = true, Code = "COD" });
            public Task<int> UpsertPaymentAsync(CommercePaymentMethod m) => Task.FromResult(1);
            public Task DeletePaymentAsync(int id) => Task.CompletedTask;
            public Task<List<CommerceDeliverySlot>> ListSlotsAsync(int? siteId, bool activeOnly = false) => Task.FromResult(new List<CommerceDeliverySlot>());
            public Task<CommerceDeliverySlot> GetSlotAsync(int id) => Task.FromResult<CommerceDeliverySlot>(null!);
            public Task<int> UpsertSlotAsync(CommerceDeliverySlot m) => Task.FromResult(1);
            public Task DeleteSlotAsync(int id) => Task.CompletedTask;
        }

        sealed class MemCart : ICommerceCartRepository
        {
            public CommerceCart Cart = new() { Id = 1, Items = new List<CommerceCartItem>() };
            public Task<CommerceCart> GetOrCreateAsync(int? siteId, int? userId, string guestKey) => Task.FromResult(Cart);
            public Task<CommerceCart> GetWithItemsAsync(int cartId) => Task.FromResult(Cart);
            public Task SetItemAsync(int cartId, int productId, string title, long unitPrice, int quantity) => Task.CompletedTask;
            public Task RemoveItemAsync(int cartId, int productId) => Task.CompletedTask;
            public Task ClearAsync(int cartId) { Cart.Items.Clear(); return Task.CompletedTask; }
            public Task AttachUserAsync(int cartId, int userId) => Task.CompletedTask;
        }

        sealed class MemCOrders : ICommerceOrderRepository
        {
            public List<(CommerceOrderDetail Order, List<OrderItem> Items)> Created = new();
            public Task<int> CreateCommerceOrderAsync(CommerceOrderDetail order, IEnumerable<OrderItem> items, int? changedByUserId)
            {
                var list = items.ToList();
                order.Id = Created.Count + 1;
                order.OrderNumber = "C" + order.Id;
                Created.Add((order, list));
                return Task.FromResult(order.Id);
            }
            public Task<CommerceOrderDetail> GetDetailAsync(int orderId)
            {
                var x = Created.FirstOrDefault(c => c.Order.Id == orderId);
                if (x.Order == null) return Task.FromResult<CommerceOrderDetail>(null!);
                x.Order.Items = x.Items;
                return Task.FromResult(x.Order);
            }
            public Task<List<CommerceOrderDetail>> ListAsync(int? siteId = null, int? userId = null, int? storeId = null, int top = 200)
                => Task.FromResult(new List<CommerceOrderDetail>());
            public Task<List<CommerceOrderDetail>> ListForStoreIdsAsync(IReadOnlyList<int> storeIds, int top = 200)
                => Task.FromResult(new List<CommerceOrderDetail>());
            public Task UpdateFulfillmentAsync(int orderId, byte status, string trackingCode, string note, int? changedByUserId) => Task.CompletedTask;
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
            public Task<bool> OrderItemColumnExistsAsync(string columnName) => Task.FromResult(true);
            public Task<bool> OrderColumnExistsAsync(string columnName) => Task.FromResult(true);
        }

        sealed class MemP : IProductService
        {
            public int Qty = 10;
            public ProductDto Extra;
            public Task<ProductDto> GetByIdAsync(int id)
            {
                if (Extra != null && Extra.Id == id) return Task.FromResult(Extra);
                return Task.FromResult(new ProductDto { Id = 1, Name = "P", IsActive = true, Price = 1000, Quantity = Qty, StoreId = 7, Sku = "SKU" });
            }
            public Task<List<ProductDto>> GetAllAsync(int? storeId = null, int? companyId = null) => Task.FromResult(new List<ProductDto>());
            public Task<int> CreateAsync(CreateProductDto dto) => Task.FromResult(0);
            public Task<bool> UpdateAsync(UpdateProductDto dto)
            {
                Qty = dto.Quantity;
                return Task.FromResult(true);
            }
            public Task<bool> DeleteAsync(int id) => Task.FromResult(true);
        }

        sealed class MemVars : ICommerceVariationRepository
        {
            public List<CommerceProductVariation> All = new()
            {
                new() { Id = 11, ProductId = 1, Sku = "RED", Stock = 5, IsActive = true, Price = 900 }
            };
            public Task<List<CommerceProductVariation>> ListAsync(int productId) => Task.FromResult(All.Where(v => v.ProductId == productId).ToList());
            public Task<CommerceProductVariation> GetAsync(int id) => Task.FromResult(All.FirstOrDefault(v => v.Id == id));
            public Task<int> InsertAsync(CommerceProductVariation m) => Task.FromResult(m.Id);
            public Task UpdateAsync(CommerceProductVariation m) => Task.CompletedTask;
            public Task DeleteAsync(int id) => Task.CompletedTask;
            public Task<bool> SkuExistsAsync(string sku, int? siteId, int? exceptVariationId) => Task.FromResult(false);
            public Task ReplaceAttributesAsync(int variationId, IReadOnlyList<CommerceVariationAttribute> attrs) => Task.CompletedTask;
            public Task<List<CommerceVariationAttribute>> ListAttributesAsync(int variationId) => Task.FromResult(new List<CommerceVariationAttribute>());
            public Task SetDefaultAsync(int productId, int variationId) => Task.CompletedTask;
            public Task DecrementStockAsync(int variationId, int quantity)
            {
                var v = All.First(x => x.Id == variationId);
                v.Stock -= quantity;
                return Task.CompletedTask;
            }
        }

        sealed class MemVarCart : ICommerceVariationCartRepository
        {
            public List<CommerceVariationCartItem> Items = new();
            public Task<CommerceVariationCartItem> GetAsync(int cartId, int variationId) =>
                Task.FromResult(Items.FirstOrDefault(i => i.VariationId == variationId));
            public Task<List<CommerceVariationCartItem>> ListAsync(int cartId) => Task.FromResult(Items.ToList());
            public Task UpsertAsync(CommerceVariationCartItem item) => Task.CompletedTask;
            public Task RemoveAsync(int cartId, int variationId) { Items.RemoveAll(i => i.VariationId == variationId); return Task.CompletedTask; }
            public Task ClearAsync(int cartId) { Items.Clear(); return Task.CompletedTask; }
        }
    }
}
