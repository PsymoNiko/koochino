using MyShop.Application.Interfaces.Commerce;
using MyShop.Application.Services.Commerce;
using MyShop.Core.Models.Commerce;

namespace MyShop.Tests
{
    public class CommercePhaseHTests
    {
        [Fact]
        public async Task FlagOff_Request_Throws()
        {
            var h = Harness.Create(false);
            await Assert.ThrowsAsync<InvalidOperationException>(() => h.Svc.RequestAsync(1, 100, "x", 1));
        }

        [Fact]
        public async Task Request_OverRemaining_Throws()
        {
            var h = Harness.Create(true);
            await Assert.ThrowsAsync<InvalidOperationException>(() => h.Svc.RequestAsync(1, 5000, "x", 1));
        }

        [Fact]
        public async Task FullComplete_MarksPaymentAndFulfillmentRefunded()
        {
            var h = Harness.Create(true);
            var id = await h.Svc.RequestAsync(1, 1000, "full", 1);
            await h.Svc.CompleteAsync(id, 1);
            Assert.Equal(CommerceRefundStatus.Completed, h.Repo.All[0].Status);
            Assert.Equal(CommercePaymentStatus.Refunded, h.Orders.Detail.PaymentStatus);
            Assert.Equal(CommerceFulfillmentStatus.Refunded, h.Orders.Detail.FulfillmentStatus);
            Assert.Equal(CommercePaymentStatus.Refunded, h.Orders.Pay.Status);
        }

        [Fact]
        public async Task Partial_DoesNotMarkFullRefund()
        {
            var h = Harness.Create(true);
            var id = await h.Svc.RequestAsync(1, 400, "part", 1);
            await h.Svc.CompleteAsync(id, 1);
            Assert.Equal(CommercePaymentStatus.Paid, h.Orders.Detail.PaymentStatus);
            Assert.Equal(CommerceFulfillmentStatus.Paid, h.Orders.Detail.FulfillmentStatus);
        }

        [Fact]
        public async Task DoubleComplete_Throws()
        {
            var h = Harness.Create(true);
            var id = await h.Svc.RequestAsync(1, 1000, "full", 1);
            await h.Svc.CompleteAsync(id, 1);
            await Assert.ThrowsAsync<InvalidOperationException>(() => h.Svc.CompleteAsync(id, 1));
        }

        [Fact]
        public async Task SecondRequest_CannotExceedRemaining()
        {
            var h = Harness.Create(true);
            await h.Svc.RequestAsync(1, 700, "a", 1);
            await Assert.ThrowsAsync<InvalidOperationException>(() => h.Svc.RequestAsync(1, 400, "b", 1));
        }

        sealed class Harness
        {
            public CommerceRefundService Svc = null!;
            public MemRefunds Repo = new();
            public MemOrders Orders = new();
            public Flag Flags = new();

            public static Harness Create(bool on)
            {
                var h = new Harness();
                h.Flags.On = on;
                h.Svc = new CommerceRefundService(h.Repo, h.Orders, h.Flags);
                return h;
            }
        }

        sealed class Flag : ICommerceFeatureFlags
        {
            public bool On;
            public bool IsEnabled(string featureKey) =>
                On && string.Equals(featureKey, CommerceConstants.Features.Refunds, StringComparison.OrdinalIgnoreCase);
        }

        sealed class MemRefunds : ICommerceRefundRepository
        {
            public List<CommerceRefund> All = new();
            int _n = 1;
            public Task<List<CommerceRefund>> ListByOrderAsync(int orderId) =>
                Task.FromResult(All.Where(x => x.OrderId == orderId).ToList());
            public Task<CommerceRefund> GetAsync(int id) =>
                Task.FromResult(All.FirstOrDefault(x => x.Id == id)!);
            public Task<int> InsertAsync(CommerceRefund m)
            {
                m.Id = _n++;
                All.Add(m);
                return Task.FromResult(m.Id);
            }
            public Task UpdateStatusAsync(int id, byte status)
            {
                var r = All.First(x => x.Id == id);
                r.Status = status;
                return Task.CompletedTask;
            }
            public Task<CommercePayment> GetLatestPaymentAsync(int orderId) =>
                Task.FromResult(new CommercePayment { Id = 9, OrderId = orderId, Amount = 1000, Status = CommercePaymentStatus.Paid });
        }

        sealed class MemOrders : ICommerceOrderRepository
        {
            public CommerceOrderDetail Detail = new()
            {
                Id = 1, IsCommerce = true, FinalAmount = 1000,
                PaymentStatus = CommercePaymentStatus.Paid,
                FulfillmentStatus = CommerceFulfillmentStatus.Paid
            };
            public CommercePayment Pay = new() { Id = 9, OrderId = 1, Amount = 1000, Status = CommercePaymentStatus.Paid };

            public Task<int> CreateCommerceOrderAsync(CommerceOrderDetail order, IEnumerable<OrderItem> items, int? changedByUserId) => Task.FromResult(1);
            public Task<CommerceOrderDetail> GetDetailAsync(int orderId) => Task.FromResult(Detail);
            public Task<List<CommerceOrderDetail>> ListAsync(int? siteId = null, int? userId = null, int? storeId = null, int top = 200)
                => Task.FromResult(new List<CommerceOrderDetail>());
            public Task<List<CommerceOrderDetail>> ListForStoreIdsAsync(IReadOnlyList<int> storeIds, int top = 200)
                => Task.FromResult(new List<CommerceOrderDetail>());
            public Task UpdateFulfillmentAsync(int orderId, byte status, string trackingCode, string note, int? changedByUserId) => Task.CompletedTask;
            public Task UpdatePaymentStatusAsync(int orderId, byte paymentStatus, byte? fulfillmentStatus = null)
            {
                Detail.PaymentStatus = paymentStatus;
                if (fulfillmentStatus.HasValue) Detail.FulfillmentStatus = fulfillmentStatus.Value;
                return Task.CompletedTask;
            }
            public Task<List<OrderStatusHistory>> ListHistoryAsync(int orderId) => Task.FromResult(new List<OrderStatusHistory>());
            public Task<int> InsertPaymentAsync(CommercePayment payment) => Task.FromResult(1);
            public Task UpdatePaymentAsync(CommercePayment payment)
            {
                Pay.Status = payment.Status;
                return Task.CompletedTask;
            }
            public Task<CommercePayment> GetPaymentByAuthorityAsync(string authority) => Task.FromResult<CommercePayment>(null!);
            public Task<CommercePayment> GetPaymentByIdAsync(int paymentId) => Task.FromResult(Pay);
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
    }
}
