using MyShop.Application.Interfaces;
using MyShop.Application.Interfaces.Commerce;
using MyShop.Application.Services.Commerce;
using MyShop.Core.Models;
using MyShop.Core.Models.Commerce;
using MyShop.Core.Routing;

namespace MyShop.Tests
{
    public class CommercePhaseKTests
    {
        [Fact]
        public void Routes_MapToCommerce()
        {
            Assert.Equal("Commerce", AdminRouteModuleMapping.GetModuleForPath("/admin/commerce/settlements"));
            Assert.Equal("Commerce", PublicRouteModuleMapping.GetModuleForPath("/storeowner/commerce-settlements"));
        }

        [Fact]
        public void Commission_UsesLineGross_NotHeader()
        {
            var a = CommerceSettlementMath.Commission(1000, 10);
            var b = CommerceSettlementMath.Commission(4000, 10);
            Assert.Equal(100, a);
            Assert.Equal(400, b);
            Assert.Equal(900, CommerceSettlementMath.Net(1000, a));
        }

        [Fact]
        public void StoreRate_OverridesDefault()
        {
            var rates = new[]
            {
                new CommerceCommissionRate { StoreId = null, SiteId = null, RatePercent = 5 },
                new CommerceCommissionRate { StoreId = 10, RatePercent = 12 }
            };
            Assert.Equal(12m, CommerceSettlementMath.ResolveRate(rates, 10, null));
            Assert.Equal(5m, CommerceSettlementMath.ResolveRate(rates, 20, null));
        }

        [Fact]
        public async Task FlagOff_NoSettle()
        {
            var h = Harness.Create(false, 10);
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                h.Svc.SettleStoreAsync(10, null, DateTime.Today, DateTime.Today.AddDays(1), 1, null));
            Assert.Empty(await h.Svc.ListSellerAsync(1));
        }

        [Fact]
        public async Task Seller_SeesOnlyOwnedSettlements()
        {
            var h = Harness.Create(true, 10);
            h.SettleRepo.Rows.Add(new CommerceSettlement { Id = 1, StoreId = 10, GrossAmount = 100, Status = 1 });
            h.SettleRepo.Rows.Add(new CommerceSettlement { Id = 2, StoreId = 20, GrossAmount = 900, Status = 1 });
            var seller = await h.Svc.ListSellerAsync(1);
            Assert.Single(seller);
            Assert.Equal(10, seller[0].StoreId);
        }

        [Fact]
        public async Task DuplicatePeriod_Rejected()
        {
            var h = Harness.Create(true, 10);
            h.Sales.Rows.Add(new CommerceStoreSalesRow { StoreId = 10, LineAmount = 5000 });
            var from = DateTime.Today;
            var to = from.AddDays(1);
            await h.Svc.SettleStoreAsync(10, null, from, to, 1, null);
            await Assert.ThrowsAsync<InvalidOperationException>(() => h.Svc.SettleStoreAsync(10, null, from, to, 1, null));
        }

        sealed class Harness
        {
            public MemSettle SettleRepo = new();
            public MemSales Sales = new();
            public CommerceSettlementService Svc = null!;

            public static Harness Create(bool on, int ownedStoreId)
            {
                var h = new Harness();
                h.Svc = new CommerceSettlementService(h.SettleRepo, h.Sales, new Flag { On = on }, new MemStores { StoreId = ownedStoreId });
                return h;
            }
        }

        sealed class Flag : ICommerceFeatureFlags
        {
            public bool On;
            public bool IsEnabled(string featureKey) =>
                On && string.Equals(featureKey, CommerceConstants.Features.Settlement, StringComparison.OrdinalIgnoreCase);
        }

        sealed class MemSales : ICommerceReportRepository
        {
            public List<CommerceStoreSalesRow> Rows { get; } = new();
            public Task<List<CommerceStoreSalesRow>> GetStoreSalesAsync(
                int? siteId, DateTime fromInclusive, DateTime toExclusive, IReadOnlyList<int> storeIds)
            {
                var q = Rows.AsEnumerable();
                if (storeIds != null) q = q.Where(r => storeIds.Contains(r.StoreId));
                return Task.FromResult(q.ToList());
            }
        }

        sealed class MemSettle : ICommerceSettlementRepository
        {
            public List<CommerceCommissionRate> Rates { get; } = new();
            public List<CommerceSettlement> Rows { get; } = new();
            int _id;

            public Task<List<CommerceCommissionRate>> ListRatesAsync() => Task.FromResult(Rates.ToList());
            public Task UpsertRateAsync(int? siteId, int? storeId, decimal ratePercent)
            {
                Rates.Add(new CommerceCommissionRate { SiteId = siteId, StoreId = storeId, RatePercent = ratePercent });
                return Task.CompletedTask;
            }
            public Task<List<CommerceSettlement>> ListSettlementsAsync(IReadOnlyList<int> storeIds)
            {
                var q = Rows.AsEnumerable();
                if (storeIds != null) q = q.Where(r => storeIds.Contains(r.StoreId));
                return Task.FromResult(q.ToList());
            }
            public Task<CommerceSettlement> GetByPeriodAsync(int storeId, DateTime periodFrom, DateTime periodTo) =>
                Task.FromResult(Rows.FirstOrDefault(r =>
                    r.StoreId == storeId && r.PeriodFrom == periodFrom && r.PeriodTo == periodTo
                    && r.Status == CommerceConstants.SettlementStatus.Settled)!);
            public Task<int> InsertAsync(CommerceSettlement row)
            {
                row.Id = ++_id;
                Rows.Add(row);
                return Task.FromResult(row.Id);
            }
            public Task VoidAsync(int id)
            {
                var r = Rows.FirstOrDefault(x => x.Id == id);
                if (r != null) r.Status = CommerceConstants.SettlementStatus.Voided;
                return Task.CompletedTask;
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
