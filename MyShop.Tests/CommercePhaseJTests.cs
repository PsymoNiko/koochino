using MyShop.Application.Interfaces;
using MyShop.Application.Interfaces.Commerce;
using MyShop.Application.Services.Commerce;
using MyShop.Core.Models;
using MyShop.Core.Models.Commerce;
using MyShop.Core.Routing;

namespace MyShop.Tests
{
    public class CommercePhaseJTests
    {
        [Fact]
        public void AdminReportRoute_MapsToCommerce()
        {
            Assert.Equal("Commerce", AdminRouteModuleMapping.GetModuleForPath("/admin/commerce/reports"));
        }

        [Fact]
        public void SellerReportRoute_MapsToCommerce()
        {
            Assert.Equal("Commerce", PublicRouteModuleMapping.GetModuleForPath("/storeowner/commerce-reports"));
        }

        [Fact]
        public void MixedOrder_EachStoreGetsOwnLines_HeaderIgnored()
        {
            var rows = CommerceReportMath.Aggregate(new[]
            {
                PaidLine(orderId: 1, lineStore: 10, headerStore: 99, amount: 1000),
                PaidLine(orderId: 1, lineStore: 20, headerStore: 99, amount: 4000),
                PaidLine(orderId: 2, lineStore: null, headerStore: 10, amount: 500)
            });
            Assert.Equal(2, rows.Count);
            var a = rows.Single(r => r.StoreId == 10);
            var b = rows.Single(r => r.StoreId == 20);
            Assert.Equal(1500, a.LineAmount);
            Assert.Equal(2, a.OrderCount);
            Assert.Equal(4000, b.LineAmount);
            Assert.Equal(1, b.OrderCount);
        }

        [Fact]
        public void UnpaidAndRefunded_AreExcluded()
        {
            var rows = CommerceReportMath.Aggregate(new[]
            {
                new CommerceReportLine
                {
                    OrderId = 1, LineStoreId = 10, LineAmount = 100,
                    PaymentStatus = CommercePaymentStatus.Unpaid,
                    FulfillmentStatus = CommerceFulfillmentStatus.AwaitingPayment
                },
                new CommerceReportLine
                {
                    OrderId = 2, LineStoreId = 10, LineAmount = 200,
                    PaymentStatus = CommercePaymentStatus.Paid,
                    FulfillmentStatus = CommerceFulfillmentStatus.Refunded
                },
                PaidLine(3, 10, null, 50)
            });
            Assert.Single(rows);
            Assert.Equal(50, rows[0].LineAmount);
        }

        [Fact]
        public void FilterOwned_HidesOtherStores()
        {
            var all = new List<CommerceStoreSalesRow>
            {
                new() { StoreId = 10, LineAmount = 100 },
                new() { StoreId = 20, LineAmount = 900 }
            };
            var mine = CommerceReportMath.FilterOwned(all, new HashSet<int> { 10 });
            Assert.Single(mine);
            Assert.Equal(10, mine[0].StoreId);
        }

        [Fact]
        public async Task FlagOff_ReturnsEmpty()
        {
            var h = Harness.Create(false, ownedStoreId: 10);
            h.Repo.Rows.Add(new CommerceStoreSalesRow { StoreId = 10, LineAmount = 5 });
            Assert.Empty(await h.Svc.GetAdminStoreSalesAsync(null, DateTime.Today, DateTime.Today.AddDays(1)));
            Assert.Empty(await h.Svc.GetSellerStoreSalesAsync(1, null, DateTime.Today, DateTime.Today.AddDays(1)));
        }

        [Fact]
        public async Task Seller_SeesOnlyOwnedStoreRows()
        {
            var h = Harness.Create(true, ownedStoreId: 10);
            h.Repo.Rows.Add(new CommerceStoreSalesRow { StoreId = 10, LineAmount = 100 });
            h.Repo.Rows.Add(new CommerceStoreSalesRow { StoreId = 20, LineAmount = 900 });
            var admin = await h.Svc.GetAdminStoreSalesAsync(null, DateTime.Today, DateTime.Today.AddDays(1));
            Assert.Equal(2, admin.Count);
            var seller = await h.Svc.GetSellerStoreSalesAsync(1, null, DateTime.Today, DateTime.Today.AddDays(1));
            Assert.Single(seller);
            Assert.Equal(10, seller[0].StoreId);
            Assert.Equal(new[] { 10 }, h.Repo.LastStoreFilter);
        }

        static CommerceReportLine PaidLine(int orderId, int? lineStore, int? headerStore, long amount) => new()
        {
            OrderId = orderId,
            LineStoreId = lineStore,
            HeaderStoreId = headerStore,
            LineAmount = amount,
            PaymentStatus = CommercePaymentStatus.Paid,
            FulfillmentStatus = CommerceFulfillmentStatus.Paid
        };

        sealed class Harness
        {
            public MemRepo Repo = new();
            public CommerceReportService Svc = null!;

            public static Harness Create(bool on, int ownedStoreId)
            {
                var h = new Harness();
                h.Svc = new CommerceReportService(h.Repo, new Flag { On = on }, new MemStores { StoreId = ownedStoreId });
                return h;
            }
        }

        sealed class Flag : ICommerceFeatureFlags
        {
            public bool On;
            public bool IsEnabled(string featureKey) =>
                On && string.Equals(featureKey, CommerceConstants.Features.Reports, StringComparison.OrdinalIgnoreCase);
        }

        sealed class MemRepo : ICommerceReportRepository
        {
            public List<CommerceStoreSalesRow> Rows { get; } = new();
            public IReadOnlyList<int>? LastStoreFilter { get; private set; }

            public Task<List<CommerceStoreSalesRow>> GetStoreSalesAsync(
                int? siteId, DateTime fromInclusive, DateTime toExclusive, IReadOnlyList<int> storeIds)
            {
                LastStoreFilter = storeIds;
                return Task.FromResult(Rows.ToList());
            }
        }

        sealed class MemStores : IStoreService
        {
            public int StoreId;
            public Task<int> CountAsync() => Task.FromResult(0);
            public Task<List<Store>> LatestAsync(int count) => Task.FromResult(new List<Store>());
            public Task<List<Store>> TopSellingAsync(int count) => Task.FromResult(new List<Store>());
            public Task<List<Store>> GetByUserIdAsync(int userId) =>
                Task.FromResult(new List<Store> { new() { StoreID = StoreId, UserID = userId } });
            public Task<Store> GetByIdAsync(int storeId) => Task.FromResult<Store>(null!);
            public Task<int> CreateAsync(Store store) => Task.FromResult(0);
            public Task<bool> UpdateAsync(Store store) => Task.FromResult(false);
            public Task<(bool CanDelete, List<string> Reasons)> CanDeleteAsync(int storeId) =>
                Task.FromResult((false, new List<string>()));
            public Task<bool> DeleteAsync(int storeId) => Task.FromResult(false);
            public Task<bool> ToggleStatusAsync(int storeId, bool status) => Task.FromResult(false);
            public Task<List<Store>> GetAllAsync() => Task.FromResult(new List<Store>());
            public Task<List<Store>> GetAllForUserAsync(int userId, int? siteId = null) => GetAllAsync();
            public Task<List<Store>> GetListForPublicAsync(string search, bool onlyWithCoupon, string sortBy, int? categoryId = null, int? cityId = null) =>
                Task.FromResult(new List<Store>());
        }
    }
}
