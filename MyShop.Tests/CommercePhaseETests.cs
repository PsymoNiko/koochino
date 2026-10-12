using MyShop.Application.Interfaces.Commerce;
using MyShop.Application.Interfaces.Products;
using MyShop.Application.Services.Commerce;
using MyShop.Core.Models.Commerce;
using MyShop.Core.Models.Products;

namespace MyShop.Tests
{
    public class CommercePhaseETests
    {
        [Fact]
        public void Percent_Floors_And_Caps()
        {
            var p = new CommercePromotion
            {
                DiscountType = CommerceConstants.PromotionDiscountType.Percent,
                DiscountValue = 15,
                MaxDiscount = 100
            };
            Assert.Equal(100, CommercePromotionService.ComputeDiscount(p, 1000));
            Assert.Equal(75, CommercePromotionService.ComputeDiscount(p, 500));
        }

        [Fact]
        public void Fixed_Cannot_Exceed_Subtotal()
        {
            var p = new CommercePromotion
            {
                DiscountType = CommerceConstants.PromotionDiscountType.Fixed,
                DiscountValue = 999
            };
            Assert.Equal(200, CommercePromotionService.ComputeDiscount(p, 200));
        }

        [Fact]
        public void MinSubtotal_Blocks()
        {
            var p = new CommercePromotion
            {
                DiscountType = CommerceConstants.PromotionDiscountType.Fixed,
                DiscountValue = 50,
                MinSubtotal = 1000
            };
            Assert.Equal(0, CommercePromotionService.ComputeDiscount(p, 999));
        }

        [Fact]
        public void Expired_Not_Eligible()
        {
            var p = new CommercePromotion { IsActive = true, EndsAt = DateTime.Now.AddMinutes(-1) };
            Assert.False(CommercePromotionService.IsEligible(p, 1, 7, DateTime.Now));
        }

        [Fact]
        public async Task Quote_Picks_Best_Auto_Promo()
        {
            var repo = new MemPromo();
            repo.All.Add(new CommercePromotion { Id = 1, IsActive = true, DiscountType = "Percent", DiscountValue = 5 });
            repo.All.Add(new CommercePromotion { Id = 2, IsActive = true, DiscountType = "Fixed", DiscountValue = 80 });
            var svc = new CommercePromotionService(repo);
            var q = await svc.QuoteAsync(1, 7, 1000, null);
            Assert.Equal(2, q.PromotionId);
            Assert.Equal(80, q.DiscountAmount);
        }

        [Fact]
        public async Task Quote_Code_Overrides_Auto()
        {
            var repo = new MemPromo();
            repo.All.Add(new CommercePromotion { Id = 1, IsActive = true, DiscountType = "Fixed", DiscountValue = 80 });
            repo.All.Add(new CommercePromotion { Id = 2, IsActive = true, Code = "SAVE10", DiscountType = "Percent", DiscountValue = 10 });
            var svc = new CommercePromotionService(repo);
            var q = await svc.QuoteAsync(1, 7, 1000, "save10");
            Assert.Equal(2, q.PromotionId);
            Assert.Equal(100, q.DiscountAmount);
        }

        [Fact]
        public async Task Quote_BadCode_Throws()
        {
            var svc = new CommercePromotionService(new MemPromo());
            await Assert.ThrowsAsync<InvalidOperationException>(() => svc.QuoteAsync(1, 7, 1000, "NOPE"));
        }

        [Fact]
        public async Task FlagOff_Ignores_Promo_And_Does_Not_Quote()
        {
            var h = Harness.Create(promotions: false);
            h.Promo.WouldDiscount = 400;
            h.Carts.Cart.Items.Add(Line(2, 500));
            var r = await h.Svc.CheckoutAsync(1, 9, "g", Req("SAVE"), "https://x");
            Assert.True(r.Success);
            Assert.Equal(0, h.Orders.Created[0].Order.DiscountAmount);
            Assert.Equal(1000, h.Orders.Created[0].Order.FinalAmount);
            Assert.Equal(0, h.Promo.QuoteCalls);
        }

        [Fact]
        public async Task FlagOn_Applies_Quoted_Discount_Not_Shipping()
        {
            var h = Harness.Create(promotions: true);
            h.Catalog.Fee = 200;
            h.Promo.WouldDiscount = 100;
            h.Promo.PromoId = 3;
            h.Carts.Cart.Items.Add(Line(2, 500));
            var r = await h.Svc.CheckoutAsync(1, 9, "g", Req(), "https://x");
            Assert.True(r.Success);
            Assert.Equal(1000, h.Orders.Created[0].Order.TotalAmount);
            Assert.Equal(100, h.Orders.Created[0].Order.DiscountAmount);
            Assert.Equal(1100, h.Orders.Created[0].Order.FinalAmount);
            Assert.Single(h.Promo.Redemptions);
        }

        [Fact]
        public async Task FlagOn_BadCode_Fails_Checkout()
        {
            var h = Harness.Create(promotions: true);
            h.Promo.ThrowOnQuote = true;
            h.Carts.Cart.Items.Add(Line(1, 500));
            var r = await h.Svc.CheckoutAsync(1, 9, "g", Req("BAD"), "https://x");
            Assert.False(r.Success);
            Assert.Contains("پروموشن", r.Message);
        }

        static CommerceCartItem Line(int qty, long unit) =>
            new() { ProductId = 1, Title = "P", Quantity = qty, UnitPrice = unit };

        static CommerceCheckoutRequest Req(string code = null) => new()
        {
            ShippingMethodId = 1,
            PaymentMethodId = 1,
            ShippingAddress = "تهران",
            PromoCode = code
        };

        sealed class MemPromo : ICommercePromotionRepository, ICommercePromotionService
        {
            public List<CommercePromotion> All = new();
            public long WouldDiscount;
            public int? PromoId = 1;
            public int QuoteCalls;
            public bool ThrowOnQuote;
            public List<(int Promo, int Order, long Amount)> Redemptions = new();

            public Task<List<CommercePromotion>> ListAllAsync(int? siteId) => Task.FromResult(All);
            public Task<List<CommercePromotion>> ListActiveAsync(int? siteId, int? storeId) =>
                Task.FromResult(All.Where(p => p.IsActive && string.IsNullOrWhiteSpace(p.Code)).ToList());
            public Task<CommercePromotion> GetByCodeAsync(string code, int? siteId) =>
                Task.FromResult(All.FirstOrDefault(p => string.Equals(p.Code, code, StringComparison.OrdinalIgnoreCase)));
            public Task<CommercePromotion> GetAsync(int id) => Task.FromResult(All.FirstOrDefault(p => p.Id == id));
            public Task<int> InsertAsync(CommercePromotion m) => Task.FromResult(1);
            public Task UpdateAsync(CommercePromotion m) => Task.CompletedTask;
            public Task DeleteAsync(int id) => Task.CompletedTask;
            public Task IncrementUsageAsync(int promotionId) => Task.CompletedTask;
            public Task InsertRedemptionAsync(int promotionId, int orderId, int userId, long amount)
            {
                Redemptions.Add((promotionId, orderId, amount));
                return Task.CompletedTask;
            }

            public Task<List<CommercePromotion>> ListAsync(int? siteId) => ListAllAsync(siteId);
            public Task<int> SaveAsync(CommercePromotion m, int? siteId) => Task.FromResult(1);
            public Task DeleteAsync(int id, int? siteId) => Task.CompletedTask;
            public Task<CommercePromotionQuote> QuoteAsync(int? siteId, int? storeId, long subtotal, string promoCode)
            {
                QuoteCalls++;
                if (ThrowOnQuote) throw new InvalidOperationException("کد پروموشن نامعتبر است.");
                return Task.FromResult(new CommercePromotionQuote { PromotionId = PromoId, DiscountAmount = WouldDiscount, Title = "T" });
            }
            public Task RedeemAsync(int promotionId, int orderId, int userId, long amount) =>
                InsertRedemptionAsync(promotionId, orderId, userId, amount);
        }

        sealed class Harness
        {
            public CommerceService Svc;
            public MemCatalog Catalog = new();
            public MemCart Carts = new();
            public MemCOrders Orders = new();
            public MemP Products = new();
            public MemPromo Promo = new();
            public Flag Flags = new();

            public static Harness Create(bool promotions)
            {
                var h = new Harness();
                h.Flags.Promotions = promotions;
                h.Svc = new CommerceService(
                    h.Catalog, h.Carts, h.Orders, Array.Empty<IPaymentGateway>(),
                    h.Products, pricing: new CommercePricingService(),
                    flags: h.Flags, promotions: h.Promo);
                return h;
            }
        }

        sealed class Flag : ICommerceFeatureFlags
        {
            public bool Promotions;
            public bool IsEnabled(string featureKey) =>
                string.Equals(featureKey, CommerceConstants.Features.Promotions, StringComparison.OrdinalIgnoreCase) && Promotions;
        }

        sealed class MemCatalog : ICommerceCatalogRepository
        {
            public long Fee;
            public Task<List<CommerceShippingMethod>> ListShippingAsync(int? siteId, bool activeOnly = false) => Task.FromResult(new List<CommerceShippingMethod>());
            public Task<CommerceShippingMethod> GetShippingAsync(int id) => Task.FromResult(new CommerceShippingMethod { Id = 1, IsActive = true, Fee = Fee });
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
            public Task<CommerceOrderDetail> GetDetailAsync(int orderId)
            {
                var x = Created.FirstOrDefault(c => c.Order.Id == orderId);
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
            public Task<ProductDto> GetByIdAsync(int id) =>
                Task.FromResult(new ProductDto { Id = 1, Name = "P", IsActive = true, Price = 1000, Quantity = 99, StoreId = 7, Sku = "SKU" });
            public Task<List<ProductDto>> GetAllAsync(int? storeId = null, int? companyId = null) => Task.FromResult(new List<ProductDto>());
            public Task<int> CreateAsync(CreateProductDto dto) => Task.FromResult(0);
            public Task<bool> UpdateAsync(UpdateProductDto dto) => Task.FromResult(true);
            public Task<bool> DeleteAsync(int id) => Task.FromResult(true);
        }
    }
}
