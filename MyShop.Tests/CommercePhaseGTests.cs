using MyShop.Application.Interfaces.Commerce;
using MyShop.Application.Interfaces.Products;
using MyShop.Application.Services.Commerce;
using MyShop.Core.Models.Commerce;
using MyShop.Core.Models.Products;

namespace MyShop.Tests
{
    public class CommercePhaseGTests
    {
        [Fact]
        public void Shipping_FreeOver_ZeroesFee()
        {
            var m = new CommerceShippingMethod { Fee = 200, FreeOverAmount = 1000 };
            Assert.Equal(0, CommerceTotals.QuoteShippingFee(m, 1000, null, null));
            Assert.Equal(200, CommerceTotals.QuoteShippingFee(m, 999, null, null));
        }

        [Fact]
        public void Shipping_MultiStore_SumsRates_ThenFreeOver()
        {
            var m = new CommerceShippingMethod { Fee = 50, FreeOverAmount = 5000 };
            var rates = new[]
            {
                new CommerceStoreShippingRate { StoreId = 7, Fee = 100 },
                new CommerceStoreShippingRate { StoreId = 99, Fee = 80 }
            };
            Assert.Equal(180, CommerceTotals.QuoteShippingFee(m, 100, new[] { 7, 99 }, rates));
            Assert.Equal(0, CommerceTotals.QuoteShippingFee(m, 5000, new[] { 7, 99 }, rates));
        }

        [Fact]
        public void Tax_Exclusive_OnDiscountedSubtotal_NotShipping_ByDefault()
        {
            var rate = new CommerceTaxRate { IsActive = true, RatePercent = 9 };
            Assert.Equal(81, CommerceTotals.QuoteTax(rate, 1000, 100, 200));
        }

        [Fact]
        public void Tax_CanIncludeShipping()
        {
            var rate = new CommerceTaxRate { IsActive = true, RatePercent = 10, AppliesToShipping = true };
            Assert.Equal(120, CommerceTotals.QuoteTax(rate, 1000, 0, 200));
        }

        [Fact]
        public async Task FlagOff_NoTax_UsesMethodFee()
        {
            var h = Harness.Create(tax: false);
            h.Catalog.Fee = 150;
            h.Tax.Rate = new CommerceTaxRate { IsActive = true, RatePercent = 10 };
            h.Carts.Cart.Items.Add(new CommerceCartItem { ProductId = 1, Title = "P", Quantity = 2, UnitPrice = 500 });
            var r = await h.Svc.CheckoutAsync(1, 9, "g", Req(), "https://x");
            Assert.True(r.Success);
            Assert.Equal(0, h.Orders.Created[0].Order.TaxAmount);
            Assert.Equal(150, h.Orders.Created[0].Order.ShippingFee);
            Assert.Equal(1150, h.Orders.Created[0].Order.FinalAmount);
        }

        [Fact]
        public async Task FlagOn_AppliesTax_AndFreeShipping()
        {
            var h = Harness.Create(tax: true);
            h.Catalog.Fee = 200;
            h.Catalog.FreeOver = 1000;
            h.Tax.Rate = new CommerceTaxRate { IsActive = true, RatePercent = 10 };
            h.Carts.Cart.Items.Add(new CommerceCartItem { ProductId = 1, Title = "P", Quantity = 2, UnitPrice = 500 });
            var r = await h.Svc.CheckoutAsync(1, 9, "g", Req(), "https://x");
            Assert.True(r.Success);
            Assert.Equal(0, h.Orders.Created[0].Order.ShippingFee);
            Assert.Equal(100, h.Orders.Created[0].Order.TaxAmount);
            Assert.Equal(1100, h.Orders.Created[0].Order.FinalAmount);
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
            public MemTax Tax = new();
            public Flag Flags = new();

            public static Harness Create(bool tax)
            {
                var h = new Harness();
                h.Flags.Tax = tax;
                h.Svc = new CommerceService(
                    h.Catalog, h.Carts, h.Orders, Array.Empty<IPaymentGateway>(),
                    h.Products, pricing: new CommercePricingService(),
                    flags: h.Flags, taxRates: h.Tax);
                return h;
            }
        }

        sealed class Flag : ICommerceFeatureFlags
        {
            public bool Tax;
            public bool IsEnabled(string featureKey) =>
                string.Equals(featureKey, CommerceConstants.Features.Tax, StringComparison.OrdinalIgnoreCase) && Tax;
        }

        sealed class MemTax : ICommerceTaxRepository
        {
            public CommerceTaxRate Rate;
            public Task<List<CommerceTaxRate>> ListAsync(int? siteId) => Task.FromResult(new List<CommerceTaxRate>());
            public Task<CommerceTaxRate> GetActiveForSiteAsync(int? siteId) => Task.FromResult(Rate);
            public Task<int> UpsertAsync(CommerceTaxRate m) => Task.FromResult(1);
            public Task DeleteAsync(int id) => Task.CompletedTask;
        }

        sealed class MemCatalog : ICommerceCatalogRepository
        {
            public long Fee;
            public long? FreeOver;
            public Task<List<CommerceShippingMethod>> ListShippingAsync(int? siteId, bool activeOnly = false) => Task.FromResult(new List<CommerceShippingMethod>());
            public Task<CommerceShippingMethod> GetShippingAsync(int id) =>
                Task.FromResult(new CommerceShippingMethod { Id = 1, IsActive = true, Fee = Fee, FreeOverAmount = FreeOver });
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
            public CommerceCart Cart = new() { Id = 1, Items = new List<CommerceCartItem>(), StoreId = 7 };
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
                order.Id = Created.Count + 1;
                order.OrderNumber = "C" + order.Id;
                Created.Add((order, items.ToList()));
                return Task.FromResult(order.Id);
            }
            public Task<CommerceOrderDetail> GetDetailAsync(int orderId) =>
                Task.FromResult(Created.FirstOrDefault(c => c.Order.Id == orderId).Order);
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
            public Task<ProductDto> GetByIdAsync(int id) =>
                Task.FromResult(new ProductDto { Id = 1, Name = "P", IsActive = true, Price = 1000, Quantity = 99, StoreId = 7, Sku = "SKU" });
            public Task<List<ProductDto>> GetAllAsync(int? storeId = null, int? companyId = null) => Task.FromResult(new List<ProductDto>());
            public Task<int> CreateAsync(CreateProductDto dto) => Task.FromResult(0);
            public Task<bool> UpdateAsync(UpdateProductDto dto) => Task.FromResult(true);
            public Task<bool> DeleteAsync(int id) => Task.FromResult(true);
        }
    }
}
