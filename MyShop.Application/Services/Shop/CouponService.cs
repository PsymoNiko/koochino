using MyShop.Application.Interfaces;
using MyShop.Application.Interfaces.Finance;
using MyShop.Application.Interfaces.Settings;
using MyShop.Core.Interfaces;
using MyShop.Core.Models;
using MyShop.Core.Models.Dto;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Services
{
    public class CouponService : ICouponService
    {
        private readonly ICouponRepository _repo;
        private readonly IOrderService _orderService;
        private readonly IStoreVoucherWalletService _wallet;
        private readonly ISiteAdminSettingsService _siteSettings;
        private readonly ICurrentTenant _tenant;

        public CouponService(
            ICouponRepository repo,
            IOrderService orderService,
            IStoreVoucherWalletService wallet = null,
            ISiteAdminSettingsService siteSettings = null,
            ICurrentTenant tenant = null)
        {
            _repo = repo;
            _orderService = orderService;
            _wallet = wallet;
            _siteSettings = siteSettings;
            _tenant = tenant;
        }

        public static long FaceValue(Coupon c)
        {
            if (c == null) return 0;
            if (c.DiscountType != 0) return 0;
            var unit = c.DiscountValue ?? (long?)c.DiscountAmount ?? 0;
            var stock = c.CoponStock < 0 ? 0 : c.CoponStock;
            return unit * stock;
        }

        private async Task ClampExpireToSiteCreditAsync(Coupon coupon)
        {
            if (coupon == null) return;
            var days = 90;
            try
            {
                if (_siteSettings != null && _tenant != null && _tenant.SiteId > 0)
                {
                    var s = await _siteSettings.GetSettingsAsync(_tenant.SiteId);
                    if (s != null && s.VoucherCreditExpireDays >= 1)
                        days = s.VoucherCreditExpireDays;
                }
            }
            catch { }
            var cap = DateTime.Now.Date.AddDays(days);
            if (coupon.CoponExpireDate > cap)
                coupon.CoponExpireDate = cap;
        }

        public Task<List<Coupon>> GetByUserIdAsync(int userId) => _repo.GetByUserIdAsync(userId);
        public Task<List<Coupon>> GetByStoreIdAsync(int storeId) => _repo.GetByStoreIdAsync(storeId);
        public Task<List<Coupon>> GetAllPendingAsync() => _repo.GetAllPendingAsync();
        public Task<List<Coupon>> GetAllActiveAsync() => _repo.GetAllActiveAsync();
        public Task<List<Coupon>> GetAllAsync() => _repo.GetAllAsync();
        public Task<Coupon> GetByIdAsync(int couponId) => _repo.GetByIdAsync(couponId);

        public async Task<int> CreateAsync(Coupon coupon)
        {
            await ClampExpireToSiteCreditAsync(coupon);
            var id = await _repo.CreateAsync(coupon);
            if (id <= 0 || _wallet == null) return id;
            var r = await _wallet.TryReserveForCouponAsync(coupon.StoreID, FaceValue(coupon), id, coupon.UserID);
            if (!r.Ok)
            {
                try { await _repo.DeleteAsync(id); } catch { }
                return r.Error != null && r.Error.Contains("موجودی") ? -4 : -5;
            }
            return id;
        }

        public async Task<int> CreateSystemCouponAsync(Coupon coupon)
        {
            await ClampExpireToSiteCreditAsync(coupon);
            return await _repo.CreateAsync(coupon);
        }

        public async Task<bool> UpdateAsync(Coupon coupon)
        {
            await ClampExpireToSiteCreditAsync(coupon);
            var old = await _repo.GetByIdAsync(coupon.CoponID);
            var ok = await _repo.UpdateAsync(coupon);
            if (!ok || _wallet == null || old == null) return ok;
            var delta = FaceValue(coupon) - FaceValue(old);
            if (delta > 0)
            {
                var r = await _wallet.TryReserveForCouponAsync(coupon.StoreID, delta, coupon.CoponID, coupon.UserID);
                if (!r.Ok)
                {
                    await _repo.UpdateAsync(old);
                    return false;
                }
            }
            else if (delta < 0)
                await _wallet.TryReleaseForCouponAsync(coupon.StoreID, -delta, coupon.CoponID, coupon.UserID);
            return true;
        }

        public async Task<bool> DeleteAsync(int couponId)
        {
            var c = await _repo.GetByIdAsync(couponId);
            var ok = await _repo.DeleteAsync(couponId);
            if (ok && c != null && _wallet != null)
                await _wallet.TryReleaseForCouponAsync(c.StoreID, FaceValue(c), couponId, c.UserID);
            return ok;
        }

        public async Task<bool> ApproveByAdminAsync(int couponId, bool approved)
        {
            var c = await _repo.GetByIdAsync(couponId);
            var ok = await _repo.ApproveByAdminAsync(couponId, approved);
            if (ok && !approved && c != null && _wallet != null)
                await _wallet.TryReleaseForCouponAsync(c.StoreID, FaceValue(c), couponId, c.UserID);
            return ok;
        }

        public Task<bool> ToggleStoreStatusAsync(int couponId, bool status) => _repo.ToggleStoreStatusAsync(couponId, status);

        public Task<CouponValidateResultDto> ValidateCouponAsync(int userId, string couponCode, long purchaseAmount)
            => _repo.ValidateCouponAsync(userId, couponCode ?? "", purchaseAmount);

        public async Task<(bool Success, string Message, int OrderId)> ApplyCouponAsync(int userId, int coponId, long purchaseAmount, long discountAmount, int? storeId = null, long walletAmountApplied = 0)
        {
            var (result, couponUsageId) = await _repo.RecordCouponUsageAsync(coponId, userId, purchaseAmount, discountAmount);
            if (result != 1)
            {
                if (result == -1) return (false, "شما قبلاً از این کوپن استفاده کرده‌اید.", 0);
                return (false, "خطا در ثبت استفاده از کوپن.", 0);
            }
            var coupon = await _repo.GetByIdAsync(coponId);
            var storeIdToUse = storeId ?? coupon?.StoreID;
            if (storeIdToUse.HasValue && storeIdToUse.Value > 0)
            {
                try { await _repo.SetCouponUsageRedeemStoreAsync(couponUsageId, storeIdToUse.Value, discountAmount); }
                catch { }
            }
            return await _orderService.CreateOrderWithCouponAsync(userId, couponUsageId, storeIdToUse, purchaseAmount, discountAmount, walletAmountApplied);
        }

        public Task<List<CouponUsage>> GetMyUsagesAsync(int userId) => _repo.GetUsagesByUserIdAsync(userId);
        public Task<List<CouponUsageReportDto>> GetUsageReportForAdminAsync(int? userIdFilter, int? coponIdFilter)
            => _repo.GetUsageReportForAdminAsync(userIdFilter, coponIdFilter);
        public Task<List<CouponUsageStoreOwnerDto>> GetUsageReportByStoreOwnerAsync(int storeOwnerUserId)
            => _repo.GetUsageReportByStoreOwnerAsync(storeOwnerUserId);
        public Task<bool> ApproveCouponUsageByStoreAsync(int usageId, int storeOwnerUserId)
            => _repo.ApproveCouponUsageByStoreAsync(usageId, storeOwnerUserId);
    }
}
