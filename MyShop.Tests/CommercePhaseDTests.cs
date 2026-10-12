using MyShop.Application.Interfaces.Commerce;
using MyShop.Application.Interfaces.Products;
using MyShop.Application.Services.Commerce;
using MyShop.Core.Models.Commerce;
using MyShop.Core.Models.Products;

namespace MyShop.Tests
{
    public class CommercePhaseDTests
    {
        [Fact]
        public async Task FlagOff_DoesNotCreateReservation()
        {
            var h = Harness.Create(reservation: false);
            h.Carts.Cart.Items.Add(new CommerceCartItem { ProductId = 1, Title = "P", Quantity = 2, UnitPrice = 50 });
            var r = await h.Svc.CheckoutAsync(1, 9, "g", Req(), "https://x");
            Assert.True(r.Success);
            Assert.Empty(h.Holds.Rows);
            Assert.Equal(8, h.Products.Qty);
        }

        [Fact]
        public async Task FlagOn_SecondCheckout_BlockedWhileHoldActive()
        {
            var h = Harness.Create(reservation: true, online: true);
            h.Carts.Cart.Items.Add(new CommerceCartItem { ProductId = 1, Title = "P", Quantity = 8, UnitPrice = 50 });
            var r1 = await h.Svc.CheckoutAsync(1, 9, "g", Req(), "https://x");
            Assert.True(r1.Success);
            Assert.Equal(10, h.Products.Qty);

            h.Carts.Cart.Items.Add(new CommerceCartItem { ProductId = 1, Title = "P", Quantity = 5, UnitPrice = 50 });
            var r2 = await h.Svc.CheckoutAsync(1, 9, "g", Req(), "https://x");
            Assert.False(r2.Success);
            Assert.Contains("رزرو", r2.Message);
        }

        [Fact]
        public async Task FlagOn_ExpiredHold_DoesNotBlock()
        {
            var h = Harness.Create(reservation: true);
            h.Holds.Rows.Add(new CommerceStockReservation
            {
                Id = 99, ProductId = 1, Quantity = 9,
                Status = CommerceConstants.ReservationStatus.Active,
                ExpiresAt = DateTime.Now.AddMinutes(-1)
            });
            h.Carts.Cart.Items.Add(new CommerceCartItem { ProductId = 1, Title = "P", Quantity = 8, UnitPrice = 50 });
            var r = await h.Svc.CheckoutAsync(1, 9, "g", Req(), "https://x");
            Assert.True(r.Success);
        }

        [Fact]
        public async Task VariationHold_DoesNotConsumeSimpleProductPool()
        {
            var h = Harness.Create(reservation: true, variationCart: true);
            h.VarCart.Items.Add(new CommerceVariationCartItem
            {
                CartId = 1, ProductId = 1, VariationId = 11, Quantity = 4, UnitPriceSnapshot = 900
            });
            var r1 = await h.Svc.CheckoutAsync(1, 9, "g", Req(), "https://x");
            Assert.True(r1.Success);
            Assert.Equal(1, h.Vars.All[0].Stock);
            Assert.Equal(10, h.Products.Qty);

            h.Carts.Cart.Items.Add(new CommerceCartItem { ProductId = 1, Title = "P", Quantity = 9, UnitPrice = 50 });
            var r2 = await h.Svc.CheckoutAsync(1, 9, "g", Req(), "https://x");
            Assert.True(r2.Success);
            Assert.Equal(1, h.Products.Qty);
        }

        static CommerceCheckoutRequest Req() => new()
        {
            ShippingMethodId = 1,
            PaymentMethodId = 1,
            ShippingAddress = "تهران"
        };

        sealed class Harness
        {
            public CommerceService Svc;
            public MemCatalog Catalog = new();
            public MemCart Carts = new();
            public MemCOrders Orders = new();
            public MemP Products = new();
            public MemVars Vars = new();
            public MemVarCart VarCart = new();
            public MemHolds Holds = new();
            public Flag Flags = new();

            public static Harness Create(bool reservation, bool variationCart = false, bool online = false)
            {
                var h = new Harness();
                h.Flags.Reservation = reservation;
                h.Flags.VariationCart = variationCart;
                h.Catalog.Online = online;
                h.Holds.ProductOnHand = () => h.Products.Qty;
                h.Holds.VarOnHand = id => h.Vars.All.First(v => v.Id == id).Stock;
                IPaymentGateway[] gws = online ? new IPaymentGateway[] { new MockPaymentGateway() } : Array.Empty<IPaymentGateway>();
                h.Svc = new CommerceService(
                    h.Catalog, h.Carts, h.Orders, gws,
                    h.Products, pricing: new CommercePricingService(),
                    flags: h.Flags, varCart: h.VarCart, variations: h.Vars, reservations: h.Holds);
                return h;
            }
        }

        sealed class Flag : ICommerceFeatureFlags
        {
            public bool Reservation, VariationCart;
            public bool IsEnabled(string featureKey)
            {
                if (string.Equals(featureKey, CommerceConstants.Features.Reservation, StringComparison.OrdinalIgnoreCase))
                    return Reservation;
                if (string.Equals(featureKey, CommerceConstants.Features.VariationCart, StringComparison.OrdinalIgnoreCase))
                    return VariationCart;
                return false;
            }
        }

        sealed class MemHolds : ICommerceStockReservationRepository
        {
            public List<CommerceStockReservation> Rows = new();
            public Func<int> ProductOnHand;
            public Func<int, int> VarOnHand;
            int _n = 1;

            public Task ExpireStaleAsync()
            {
                foreach (var r in Rows.Where(x => x.Status == CommerceConstants.ReservationStatus.Active && x.ExpiresAt < DateTime.Now))
                    r.Status = CommerceConstants.ReservationStatus.Released;
                return Task.CompletedTask;
            }

            public async Task<string> TryReserveLinesAsync(int cartId, IReadOnlyList<OrderItem> items, DateTime expiresAt)
            {
                await ExpireStaleAsync();
                foreach (var it in items)
                {
                    var isVar = it.VariationId.HasValue && it.VariationId.Value > 0;
                    var onHand = isVar ? VarOnHand(it.VariationId.Value) : ProductOnHand();
                    var held = Rows.Where(x => x.Status == CommerceConstants.ReservationStatus.Active && x.ExpiresAt >= DateTime.Now
                        && (isVar ? x.VariationId == it.VariationId : x.ProductId == it.ProductId && x.VariationId == null))
                        .Sum(x => x.Quantity);
                    if (onHand - held < it.Quantity)
                        return "موجودی کافی نیست (بخشی از موجودی رزرو شده است).";
                    Rows.Add(new CommerceStockReservation
                    {
                        Id = _n++,
                        CartId = cartId,
                        ProductId = it.ProductId ?? 0,
                        VariationId = isVar ? it.VariationId : null,
                        Quantity = it.Quantity,
                        Status = CommerceConstants.ReservationStatus.Active,
                        ExpiresAt = expiresAt
                    });
                }
                return null;
            }

            public Task BindOrderAsync(int cartId, int orderId)
            {
                foreach (var r in Rows.Where(x => x.CartId == cartId && x.Status == CommerceConstants.ReservationStatus.Active))
                    r.OrderId = orderId;
                return Task.CompletedTask;
            }

            public Task ReleaseByCartAsync(int cartId)
            {
                foreach (var r in Rows.Where(x => x.CartId == cartId && x.Status == CommerceConstants.ReservationStatus.Active))
                    r.Status = CommerceConstants.ReservationStatus.Released;
                return Task.CompletedTask;
            }

            public Task ReleaseByOrderAsync(int orderId)
            {
                foreach (var r in Rows.Where(x => x.OrderId == orderId && x.Status == CommerceConstants.ReservationStatus.Active))
                    r.Status = CommerceConstants.ReservationStatus.Released;
                return Task.CompletedTask;
            }

            public Task<bool> HasActiveAsync(int orderId) =>
                Task.FromResult(Rows.Any(x => x.OrderId == orderId && x.Status == CommerceConstants.ReservationStatus.Active && x.ExpiresAt >= DateTime.Now));
            public Task<bool> HasCommittedAsync(int orderId) =>
                Task.FromResult(Rows.Any(x => x.OrderId == orderId && x.Status == CommerceConstants.ReservationStatus.Committed));
            public Task MarkCommittedAsync(int orderId)
            {
                foreach (var r in Rows.Where(x => x.OrderId == orderId && x.Status == CommerceConstants.ReservationStatus.Active))
                    r.Status = CommerceConstants.ReservationStatus.Committed;
                return Task.CompletedTask;
            }
        }

        sealed class MemCatalog : ICommerceCatalogRepository
        {
            public bool Online;
            public Task<List<CommerceShippingMethod>> ListShippingAsync(int? siteId, bool activeOnly = false) => Task.FromResult(new List<CommerceShippingMethod>());
            public Task<CommerceShippingMethod> GetShippingAsync(int id) => Task.FromResult(new CommerceShippingMethod { Id = 1, IsActive = true, Fee = 0 });
            public Task<int> UpsertShippingAsync(CommerceShippingMethod m) => Task.FromResult(1);
            public Task DeleteShippingAsync(int id) => Task.CompletedTask;
            public Task<List<CommercePaymentMethod>> ListPaymentAsync(int? siteId, bool activeOnly = false) => Task.FromResult(new List<CommercePaymentMethod>());
            public Task<CommercePaymentMethod> GetPaymentAsync(int id) =>
                Task.FromResult(new CommercePaymentMethod { Id = 1, IsActive = true, Code = Online ? CommercePaymentCodes.OnlineGateway : "COD", GatewayProvider = "Mock" });
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
            public Task<ProductDto> GetByIdAsync(int id) =>
                Task.FromResult(new ProductDto { Id = 1, Name = "P", IsActive = true, Price = 1000, Quantity = Qty, StoreId = 7, Sku = "SKU" });
            public Task<List<ProductDto>> GetAllAsync(int? storeId = null, int? companyId = null) => Task.FromResult(new List<ProductDto>());
            public Task<int> CreateAsync(CreateProductDto dto) => Task.FromResult(0);
            public Task<bool> UpdateAsync(UpdateProductDto dto) { Qty = dto.Quantity; return Task.FromResult(true); }
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
            public Task<CommerceVariationCartItem> GetAsync(int cartId, int variationId) => Task.FromResult(Items.FirstOrDefault(i => i.VariationId == variationId));
            public Task<List<CommerceVariationCartItem>> ListAsync(int cartId) => Task.FromResult(Items.ToList());
            public Task UpsertAsync(CommerceVariationCartItem item) => Task.CompletedTask;
            public Task RemoveAsync(int cartId, int variationId) { Items.RemoveAll(i => i.VariationId == variationId); return Task.CompletedTask; }
            public Task ClearAsync(int cartId) { Items.Clear(); return Task.CompletedTask; }
        }
    }
}
