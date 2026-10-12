using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyShop.Application.Interfaces;
using MyShop.Application.Interfaces.Commerce;
using MyShop.Core.Models;
using MyShop.Core.Models.Commerce;

namespace MyShop.Application.Services.Commerce
{
    public class CommerceSettlementService : ICommerceSettlementService
    {
        private readonly ICommerceSettlementRepository _repo;
        private readonly ICommerceReportRepository _sales;
        private readonly ICommerceFeatureFlags _flags;
        private readonly IStoreService _stores;

        public CommerceSettlementService(
            ICommerceSettlementRepository repo,
            ICommerceReportRepository sales,
            ICommerceFeatureFlags flags = null,
            IStoreService stores = null)
        {
            _repo = repo;
            _sales = sales;
            _flags = flags;
            _stores = stores;
        }

        public bool IsEnabled =>
            _flags != null && _flags.IsEnabled(CommerceConstants.Features.Settlement);

        public async Task<List<CommerceCommissionRate>> ListRatesAsync()
        {
            if (!IsEnabled) return new List<CommerceCommissionRate>();
            try { return await _repo.ListRatesAsync() ?? new List<CommerceCommissionRate>(); }
            catch { return new List<CommerceCommissionRate>(); }
        }

        public async Task SaveRateAsync(int? siteId, int? storeId, decimal ratePercent)
        {
            if (!IsEnabled) throw new InvalidOperationException("تسویه Commerce فعال نیست.");
            ratePercent = CommerceSettlementMath.ClampRate(ratePercent);
            try { await _repo.UpsertRateAsync(siteId, storeId, ratePercent); }
            catch { throw new InvalidOperationException("تسویه روی این پایگاه داده آماده نیست. ابتدا اسکریپت Phase 12 را اعمال کنید."); }
        }

        public async Task<List<CommerceSettlementPreview>> PreviewAsync(
            int? siteId, DateTime fromInclusive, DateTime toExclusive)
        {
            if (!IsEnabled) return new List<CommerceSettlementPreview>();
            try
            {
                var sales = await _sales.GetStoreSalesAsync(siteId, fromInclusive, toExclusive, null)
                            ?? new List<CommerceStoreSalesRow>();
                var rates = await _repo.ListRatesAsync() ?? new List<CommerceCommissionRate>();
                var list = new List<CommerceSettlementPreview>();
                foreach (var s in sales)
                {
                    var rate = CommerceSettlementMath.ResolveRate(rates, s.StoreId, siteId);
                    var commission = CommerceSettlementMath.Commission(s.LineAmount, rate);
                    var existing = await _repo.GetByPeriodAsync(s.StoreId, fromInclusive, toExclusive);
                    list.Add(new CommerceSettlementPreview
                    {
                        StoreId = s.StoreId,
                        StoreName = s.StoreName,
                        GrossAmount = s.LineAmount,
                        RatePercent = rate,
                        CommissionAmount = commission,
                        NetAmount = CommerceSettlementMath.Net(s.LineAmount, commission),
                        AlreadySettled = existing != null
                    });
                }
                return list;
            }
            catch
            {
                return new List<CommerceSettlementPreview>();
            }
        }

        public async Task<int> SettleStoreAsync(
            int storeId, int? siteId, DateTime fromInclusive, DateTime toExclusive, int? userId, string notes)
        {
            if (!IsEnabled) throw new InvalidOperationException("تسویه Commerce فعال نیست.");
            if (storeId <= 0) throw new InvalidOperationException("فروشگاه نامعتبر است.");
            if (toExclusive <= fromInclusive) throw new InvalidOperationException("بازه تاریخ نامعتبر است.");

            CommerceSettlement existing;
            try { existing = await _repo.GetByPeriodAsync(storeId, fromInclusive, toExclusive); }
            catch { throw new InvalidOperationException("تسویه روی این پایگاه داده آماده نیست. ابتدا اسکریپت Phase 12 را اعمال کنید."); }
            if (existing != null)
                throw new InvalidOperationException("این بازه برای این فروشگاه قبلاً تسویه شده است.");

            List<CommerceStoreSalesRow> sales;
            try
            {
                sales = await _sales.GetStoreSalesAsync(siteId, fromInclusive, toExclusive, new[] { storeId })
                        ?? new List<CommerceStoreSalesRow>();
            }
            catch { sales = new List<CommerceStoreSalesRow>(); }

            var gross = sales.Where(x => x.StoreId == storeId).Sum(x => x.LineAmount);
            if (gross <= 0)
                throw new InvalidOperationException("فروش پرداخت‌شده‌ای در این بازه نیست.");

            var rates = await _repo.ListRatesAsync() ?? new List<CommerceCommissionRate>();
            var rate = CommerceSettlementMath.ResolveRate(rates, storeId, siteId);
            var commission = CommerceSettlementMath.Commission(gross, rate);
            var row = new CommerceSettlement
            {
                StoreId = storeId,
                PeriodFrom = fromInclusive,
                PeriodTo = toExclusive,
                GrossAmount = gross,
                RatePercent = rate,
                CommissionAmount = commission,
                NetAmount = CommerceSettlementMath.Net(gross, commission),
                Status = CommerceConstants.SettlementStatus.Settled,
                SettledByUserId = userId,
                Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim()
            };
            return await _repo.InsertAsync(row);
        }

        public async Task VoidAsync(int settlementId)
        {
            if (!IsEnabled) throw new InvalidOperationException("تسویه Commerce فعال نیست.");
            if (settlementId <= 0) throw new InvalidOperationException("رکورد نامعتبر است.");
            try { await _repo.VoidAsync(settlementId); }
            catch { throw new InvalidOperationException("باطل کردن تسویه ممکن نشد."); }
        }

        public async Task<List<CommerceSettlement>> ListAdminAsync()
        {
            if (!IsEnabled) return new List<CommerceSettlement>();
            try { return await _repo.ListSettlementsAsync(null) ?? new List<CommerceSettlement>(); }
            catch { return new List<CommerceSettlement>(); }
        }

        public async Task<List<CommerceSettlement>> ListSellerAsync(int sellerUserId)
        {
            if (!IsEnabled || sellerUserId <= 0)
                return new List<CommerceSettlement>();
            var owned = await OwnedStoreIdsAsync(sellerUserId);
            if (owned.Count == 0) return new List<CommerceSettlement>();
            try
            {
                var rows = await _repo.ListSettlementsAsync(owned) ?? new List<CommerceSettlement>();
                var set = owned.ToHashSet();
                return rows.Where(r => set.Contains(r.StoreId)).ToList();
            }
            catch { return new List<CommerceSettlement>(); }
        }

        async Task<List<int>> OwnedStoreIdsAsync(int sellerUserId)
        {
            if (_stores == null) return new List<int>();
            var owned = await _stores.GetByUserIdAsync(sellerUserId) ?? new List<Store>();
            return owned
                .Select(s => s.StoreID != 0 ? s.StoreID : s.Id)
                .Where(id => id > 0)
                .Distinct()
                .ToList();
        }
    }
}
