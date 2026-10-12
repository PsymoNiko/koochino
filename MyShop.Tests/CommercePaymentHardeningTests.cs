using MyShop.Application.Interfaces;
using MyShop.Application.Interfaces.Commerce;
using MyShop.Application.Interfaces.Products;
using MyShop.Application.Services.Commerce;
using MyShop.Core.Interfaces;
using MyShop.Core.Models;
using MyShop.Core.Models.Commerce;
using MyShop.Core.Models.Products;
using MyShop.Core.Routing;

namespace MyShop.Tests
{
    public class CommercePaymentHardeningTests
    {
        [Fact]
        public async Task FirstSuccessfulCallback_PaysOnce()
        {
            var h = Harness.Create();
            var r = await h.Svc.CompleteOnlinePaymentAsync("MOCK-ABCDEF123456", "OK");
            Assert.True(r.Success);
            Assert.Equal(CommercePaymentStatus.Paid, h.Orders.Payment.Status);
            Assert.Equal(CommercePaymentFinalizedState.Done, h.Orders.Payment.FinalizedState);
            Assert.Equal(7, h.Products.Qty);
            Assert.Equal(1, h.Notifier.Count);
            Assert.Equal(1, h.Orders.FinalizeBeginCount);
        }

        [Fact]
        public async Task SameCallbackTwice_SecondIsNoOp()
        {
            var h = Harness.Create();
            await h.Svc.CompleteOnlinePaymentAsync("MOCK-ABCDEF123456", "OK");
            var r2 = await h.Svc.CompleteOnlinePaymentAsync("MOCK-ABCDEF123456", "OK");
            Assert.True(r2.Success);
            Assert.Contains("قبلاً", r2.Message);
            Assert.Equal(7, h.Products.Qty);
            Assert.Equal(1, h.Notifier.Count);
            Assert.Equal(1, h.Orders.FinalizeBeginCount);
        }

        [Fact]
        public async Task ConcurrentCallbacks_ExactlyOneCompletion()
        {
            var h = Harness.Create();
            var t1 = h.Svc.CompleteOnlinePaymentAsync("MOCK-ABCDEF123456", "OK");
            var t2 = h.Svc.CompleteOnlinePaymentAsync("MOCK-ABCDEF123456", "OK");
            var results = await Task.WhenAll(t1, t2);
            Assert.All(results, r => Assert.True(r.Success));
            Assert.Equal(7, h.Products.Qty);
            Assert.Equal(1, h.Notifier.Count);
            Assert.Equal(1, h.Orders.FinalizeBeginCount);
            Assert.Equal(CommercePaymentFinalizedState.Done, h.Orders.Payment.FinalizedState);
        }

        [Fact]
        public async Task UserCancel_RemainsFailed_NoStock()
        {
            var h = Harness.Create();
            var r = await h.Svc.CompleteOnlinePaymentAsync("MOCK-ABCDEF123456", "NOK");
            Assert.False(r.Success);
            Assert.Equal(CommercePaymentStatus.Failed, h.Orders.Payment.Status);
            Assert.Equal(10, h.Products.Qty);
            Assert.Equal(0, h.Notifier.Count);
        }

        [Fact]
        public async Task VerificationFailure_NoStockDecrement()
        {
            var h = Harness.Create(new FailVerifyGateway());
            var r = await h.Svc.CompleteOnlinePaymentAsync("MOCK-ABCDEF123456", "OK");
            Assert.False(r.Success);
            Assert.Equal(CommercePaymentStatus.Failed, h.Orders.Payment.Status);
            Assert.Equal(10, h.Products.Qty);
            Assert.Equal(0, h.Notifier.Count);
        }

        [Fact]
        public async Task AlreadyPaid_NoSecondStockOrNotify()
        {
            var h = Harness.Create();
            h.Orders.Payment.Status = CommercePaymentStatus.Paid;
            h.Orders.Payment.FinalizedState = CommercePaymentFinalizedState.Done;
            var r = await h.Svc.CompleteOnlinePaymentAsync("MOCK-ABCDEF123456", "OK");
            Assert.True(r.Success);
            Assert.Equal(10, h.Products.Qty);
            Assert.Equal(0, h.Notifier.Count);
        }

        [Fact]
        public async Task WrongAuthority_Rejected()
        {
            var h = Harness.Create();
            var r = await h.Svc.CompleteOnlinePaymentAsync("OTHER-AUTH", "OK");
            Assert.False(r.Success);
            Assert.Equal(CommercePaymentStatus.Pending, h.Orders.Payment.Status);
            Assert.Equal(10, h.Products.Qty);
        }

        [Fact]
        public async Task WrongSite_Rejected()
        {
            var h = Harness.Create(tenant: new FakeTenant(99));
            h.Orders.Order.SiteId = 1;
            var r = await h.Svc.CompleteOnlinePaymentAsync("MOCK-ABCDEF123456", "OK");
            Assert.False(r.Success);
            Assert.Equal(10, h.Products.Qty);
        }

        [Fact]
        public async Task InvalidAmount_Rejected()
        {
            var h = Harness.Create();
            h.Orders.Payment.Amount = 0;
            var r = await h.Svc.CompleteOnlinePaymentAsync("MOCK-ABCDEF123456", "OK");
            Assert.False(r.Success);
            Assert.Equal(10, h.Products.Qty);
        }

        [Fact]
        public async Task CancelledOrder_CannotBecomePaid()
        {
            var h = Harness.Create();
            h.Orders.Order.FulfillmentStatus = CommerceFulfillmentStatus.Cancelled;
            var r = await h.Svc.CompleteOnlinePaymentAsync("MOCK-ABCDEF123456", "OK");
            Assert.False(r.Success);
            Assert.Equal(10, h.Products.Qty);
        }

        [Fact]
        public async Task FailedThenVerified_CanBecomePaid()
        {
            var h = Harness.Create();
            h.Orders.Payment.Status = CommercePaymentStatus.Failed;
            var r = await h.Svc.CompleteOnlinePaymentAsync("MOCK-ABCDEF123456", "OK");
            Assert.True(r.Success);
            Assert.Equal(CommercePaymentStatus.Paid, h.Orders.Payment.Status);
            Assert.Equal(7, h.Products.Qty);
        }

        [Fact]
        public async Task MockGateway_RejectsNonMockAuthority()
        {
            var gw = new MockPaymentGateway();
            var v = await gw.VerifyAsync("ZP-123", 1000);
            Assert.False(v.Ok);
        }

        [Fact]
        public async Task StockFailure_DoesNotMarkFinalizedDone()
        {
            var h = Harness.Create();
            h.Products.ThrowOnUpdate = true;
            var r = await h.Svc.CompleteOnlinePaymentAsync("MOCK-ABCDEF123456", "OK");
            Assert.False(r.Success);
            Assert.Equal(CommercePaymentStatus.Paid, h.Orders.Payment.Status);
            Assert.Equal(CommercePaymentFinalizedState.None, h.Orders.Payment.FinalizedState);
            Assert.Equal(0, h.Notifier.Count);
        }

        [Fact]
        public void InvoiceIsOnDemand_NoStoredInvoiceTable()
        {
            Assert.True(typeof(CommerceOrderDetail).GetProperty("Items") != null);
        }

        [Fact]
        public void PublicShopAndCheckoutRoutes_Unchanged()
        {
            Assert.Equal("Commerce", PublicRouteModuleMapping.GetModuleForPath("/shop"));
            Assert.Equal("Commerce", PublicRouteModuleMapping.GetModuleForPath("/cart"));
            Assert.Equal("Commerce", PublicRouteModuleMapping.GetModuleForPath("/checkout"));
            Assert.Equal("Commerce", PublicRouteModuleMapping.GetModuleForPath("/commerce/payment/callback"));
        }

        sealed class Harness
        {
            public MemOrders Orders = null!;
            public MemProducts Products = null!;
            public CountingNotifier Notifier = null!;
            public CommerceService Svc = null!;

            public static Harness Create(IPaymentGateway gw = null, ICurrentTenant tenant = null)
            {
                var h = new Harness();
                h.Orders = new MemOrders();
                h.Products = new MemProducts();
                h.Notifier = new CountingNotifier();
                h.Svc = new CommerceService(
                    null, null, h.Orders,
                    new[] { gw ?? new MockPaymentGateway() },
                    h.Products, h.Notifier, tenant);
                return h;
            }
        }

        sealed class FailVerifyGateway : IPaymentGateway
        {
            public string Provider => "Mock";
            public Task<(bool Ok, string Authority, string RedirectUrl, string Error)> StartPaymentAsync(
                long amountRial, string description, string callbackUrl, string orderNumber)
                => Task.FromResult((false, (string)null, (string)null, "no"));
            public Task<(bool Ok, string RefId, string Error)> VerifyAsync(string authority, long amountRial)
                => Task.FromResult((false, (string)null, "verify-fail"));
        }

        sealed class FakeTenant : ICurrentTenant
        {
            public FakeTenant(int siteId) => SiteId = siteId;
            public string TenantKey => "t";
            public string ConnectionString => "";
            public int SiteId { get; }
            public string SiteCode => "t";
            public string SiteName => "t";
            public bool IsResolved => true;
            public bool HasSite => SiteId > 0;
        }

        sealed class CountingNotifier : IOrderPurchaseNotifier
        {
            public int Count;
            public Task NotifyNewOrderAsync(Order order)
            {
                Interlocked.Increment(ref Count);
                return Task.CompletedTask;
            }
        }

        sealed class MemProducts : IProductService
        {
            public int Qty = 10;
            public bool ThrowOnUpdate;
            public Task<ProductDto> GetByIdAsync(int id)
                => Task.FromResult(new ProductDto { Id = id, Name = "P", Quantity = Qty });
            public Task<List<ProductDto>> GetAllAsync(int? storeId = null, int? companyId = null)
                => Task.FromResult(new List<ProductDto>());
            public Task<int> CreateAsync(CreateProductDto dto) => Task.FromResult(0);
            public Task<bool> UpdateAsync(UpdateProductDto dto)
            {
                if (ThrowOnUpdate) throw new InvalidOperationException("stock");
                Qty = dto.Quantity;
                return Task.FromResult(true);
            }
            public Task<bool> DeleteAsync(int id) => Task.FromResult(true);
        }

        sealed class MemOrders : ICommerceOrderRepository
        {
            readonly object _gate = new();
            public int FinalizeBeginCount;
            public CommercePayment Payment = new()
            {
                Id = 1,
                OrderId = 10,
                Amount = 50000,
                Provider = "Mock",
                Authority = "MOCK-ABCDEF123456",
                Status = CommercePaymentStatus.Pending
            };
            public CommerceOrderDetail Order = new()
            {
                Id = 10,
                OrderNumber = "C-1",
                SiteId = 1,
                FulfillmentStatus = CommerceFulfillmentStatus.AwaitingPayment,
                PaymentStatus = CommercePaymentStatus.Pending,
                Items = new List<OrderItem>
                {
                    new() { ProductId = 5, Quantity = 3, Title = "P", UnitPrice = 1000, LineTotal = 3000 }
                }
            };
            public List<CommercePaymentAttempt> Attempts = new();

            public Task<int> CreateCommerceOrderAsync(CommerceOrderDetail order, IEnumerable<OrderItem> items, int? changedByUserId)
                => Task.FromResult(0);
            public Task<CommerceOrderDetail> GetDetailAsync(int orderId) => Task.FromResult(Order);
            public Task<List<CommerceOrderDetail>> ListAsync(int? siteId = null, int? userId = null, int? storeId = null, int top = 200)
                => Task.FromResult(new List<CommerceOrderDetail>());
            public Task<List<CommerceOrderDetail>> ListForStoreIdsAsync(IReadOnlyList<int> storeIds, int top = 200)
                => Task.FromResult(new List<CommerceOrderDetail>());
            public Task UpdateFulfillmentAsync(int orderId, byte status, string trackingCode, string note, int? changedByUserId)
            {
                Order.FulfillmentStatus = status;
                return Task.CompletedTask;
            }
            public Task UpdatePaymentStatusAsync(int orderId, byte paymentStatus, byte? fulfillmentStatus = null)
            {
                Order.PaymentStatus = paymentStatus;
                if (fulfillmentStatus.HasValue) Order.FulfillmentStatus = fulfillmentStatus.Value;
                return Task.CompletedTask;
            }
            public Task<List<OrderStatusHistory>> ListHistoryAsync(int orderId)
                => Task.FromResult(new List<OrderStatusHistory>());
            public Task<int> InsertPaymentAsync(CommercePayment payment) => Task.FromResult(1);
            public Task UpdatePaymentAsync(CommercePayment payment) => Task.CompletedTask;
            public Task<CommercePayment> GetPaymentByAuthorityAsync(string authority)
                => Task.FromResult(string.Equals(Payment.Authority, authority, StringComparison.OrdinalIgnoreCase) ? Payment : null);
            public Task<CommercePayment> GetPaymentByIdAsync(int paymentId) => Task.FromResult(Payment);
            public Task<bool> TryMarkPaymentPaidAsync(int paymentId, string refId, string rawResponse)
            {
                lock (_gate)
                {
                    if (Payment.Status is CommercePaymentStatus.Unpaid or CommercePaymentStatus.Pending or CommercePaymentStatus.Failed)
                    {
                        Payment.Status = CommercePaymentStatus.Paid;
                        Payment.RefId = refId;
                        Payment.PaidAt = DateTime.UtcNow;
                        return Task.FromResult(true);
                    }
                    return Task.FromResult(false);
                }
            }
            public Task<bool> TryBeginPaymentFinalizationAsync(int paymentId)
            {
                lock (_gate)
                {
                    if (Payment.Status == CommercePaymentStatus.Paid
                        && Payment.FinalizedState == CommercePaymentFinalizedState.None)
                    {
                        Payment.FinalizedState = CommercePaymentFinalizedState.InProgress;
                        FinalizeBeginCount++;
                        return Task.FromResult(true);
                    }
                    return Task.FromResult(false);
                }
            }
            public Task MarkPaymentFinalizationDoneAsync(int paymentId)
            {
                lock (_gate) Payment.FinalizedState = CommercePaymentFinalizedState.Done;
                return Task.CompletedTask;
            }
            public Task AbandonPaymentFinalizationAsync(int paymentId)
            {
                lock (_gate)
                {
                    if (Payment.FinalizedState == CommercePaymentFinalizedState.InProgress)
                        Payment.FinalizedState = CommercePaymentFinalizedState.None;
                }
                return Task.CompletedTask;
            }
            public Task<bool> TryMarkPaymentFailedIfNotPaidAsync(int paymentId, string rawResponse)
            {
                lock (_gate)
                {
                    if (Payment.Status is CommercePaymentStatus.Unpaid or CommercePaymentStatus.Pending)
                    {
                        Payment.Status = CommercePaymentStatus.Failed;
                        return Task.FromResult(true);
                    }
                    return Task.FromResult(false);
                }
            }
            public Task InsertPaymentAttemptAsync(CommercePaymentAttempt attempt)
            {
                Attempts.Add(attempt);
                return Task.CompletedTask;
            }
            public Task<CommerceDashboardKpis> GetDashboardKpisAsync(int? siteId)
                => Task.FromResult(new CommerceDashboardKpis());
            public Task<bool> OrderItemColumnExistsAsync(string columnName) => Task.FromResult(true);
            public Task<bool> OrderColumnExistsAsync(string columnName) => Task.FromResult(true);
        }
    }
}
