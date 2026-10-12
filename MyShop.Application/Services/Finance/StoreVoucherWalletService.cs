using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyShop.Application.Interfaces.Finance;
using MyShop.Core.Models.Finance;

namespace MyShop.Application.Services.Finance
{
    public class StoreVoucherWalletService : IStoreVoucherWalletService
    {
        private readonly IStoreVoucherWalletRepository _repo;
        private readonly IStoreBillingRepository _billing;
        public StoreVoucherWalletService(IStoreVoucherWalletRepository repo, IStoreBillingRepository billing)
        {
            _repo = repo;
            _billing = billing;
        }

        public Task<StoreVoucherWalletDto> GetAsync(int storeId) => _repo.GetAsync(storeId);
        public Task<List<StoreVoucherLedgerDto>> ListLedgerAsync(int storeId, int top = 200) => _repo.ListLedgerAsync(storeId, top);
        public Task<List<StoreVoucherSettlementDto>> ListSettlementsAsync(int? storeId, bool asRedeemer, string status = null)
            => _repo.ListSettlementsAsync(storeId, asRedeemer, status);
        public async Task<StoreVoucherSummaryDto> GetSummaryAsync()
        {
            var head = await _repo.GetSummaryAsync() ?? new StoreVoucherSummaryDto();
            try
            {
                var br = await _repo.GetBreakageSummaryAsync();
                head.TotalBreakage = br.TotalBreakage;
                head.BreakageLotCount = br.LotCount;
                head.NearExpiry = await _repo.ListNearExpiryAsync(7);
            }
            catch { }
            return head;
        }
        public async Task<(int Count, long Amount)> ExpireBreakageAsync()
        {
            var a = await _repo.ExpireBreakageAsync();
            var b = await _repo.ExpireReservedCouponsAsync();
            return (a.Count + b.Count, a.Amount + b.Amount);
        }

        public async Task<StoreBillingActionResult> TopUpAsync(int storeId, long amount, int? actorUserId, string note, int? expireDays = null, bool asAdmin = false)
        {
            var settings = await _billing.GetSettingsAsync() ?? new StoreBillingSettingsDto();
            if (!asAdmin && !settings.AllowMerchantTopUp)
                return StoreBillingActionResult.Fail("شارژ شخصی کیف پول در تنظیمات مالی غیرفعال است.");
            if (storeId <= 0 || amount <= 0)
                return StoreBillingActionResult.Fail("مبلغ و فروشگاه نامعتبر است.");
            var days = expireDays.GetValueOrDefault(90);
            if (days < 1) days = 1;
            if (days > 730) days = 730;
            var id = await _repo.TopUpAsync(storeId, amount, DateTime.Now.AddDays(days), actorUserId, note);
            return id > 0 ? StoreBillingActionResult.Success(id, "شارژ ثبت شد.") : StoreBillingActionResult.Fail("شارژ ناموفق بود.");
        }

        public async Task<StoreBillingActionResult> TryReserveForCouponAsync(int storeId, long faceValue, int? coponId, int? actorUserId)
        {
            var settings = await _billing.GetSettingsAsync() ?? new StoreBillingSettingsDto();
            if (!settings.VoucherEnabled) return StoreBillingActionResult.Success(0, "کیف پول خاموش است.");
            if (faceValue <= 0) return StoreBillingActionResult.Fail("برای کیف پول فقط مبلغ ثابت به تومان با موجودی معتبر مجاز است.");
            var id = await _repo.ReserveAsync(storeId, faceValue, coponId, actorUserId);
            if (id == -1) return StoreBillingActionResult.Fail("موجودی کیف پول کافی نیست.");
            return id > 0 ? StoreBillingActionResult.Success(id) : StoreBillingActionResult.Fail("رزرو اعتبار ناموفق بود.");
        }

        public async Task TryReleaseForCouponAsync(int storeId, long faceValue, int? coponId, int? actorUserId)
        {
            if (faceValue <= 0 || storeId <= 0) return;
            try { await _repo.ReleaseAsync(storeId, faceValue, coponId, null, actorUserId); }
            catch { }
        }
    }
}
