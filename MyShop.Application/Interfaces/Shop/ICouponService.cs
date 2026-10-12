using MyShop.Core.Models;
using MyShop.Core.Models.Dto;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Interfaces
{
    public interface ICouponService
    {
        Task<List<Coupon>> GetByUserIdAsync(int userId);
        Task<List<Coupon>> GetByStoreIdAsync(int storeId);
        Task<List<Coupon>> GetAllPendingAsync();
        Task<List<Coupon>> GetAllActiveAsync();
        Task<List<Coupon>> GetAllAsync();
        Task<Coupon> GetByIdAsync(int couponId);
        Task<int> CreateAsync(Coupon coupon);
        /// <summary>ایجاد کوپن سیستمی/پلتفرمی بدون رزرو کیف‌پول فروشگاه (مثلاً جایزهٔ جعبه شانس با دامنهٔ همهٔ فروشگاه‌ها).</summary>
        Task<int> CreateSystemCouponAsync(Coupon coupon);
        Task<bool> UpdateAsync(Coupon coupon);
        Task<bool> DeleteAsync(int couponId);
        Task<bool> ApproveByAdminAsync(int couponId, bool approved);
        Task<bool> ToggleStoreStatusAsync(int couponId, bool status);
        Task<CouponValidateResultDto> ValidateCouponAsync(int userId, string couponCode, long purchaseAmount);
        Task<(bool Success, string Message, int OrderId)> ApplyCouponAsync(int userId, int coponId, long purchaseAmount, long discountAmount, int? storeId = null, long walletAmountApplied = 0);
        Task<List<CouponUsage>> GetMyUsagesAsync(int userId);
        Task<List<CouponUsageReportDto>> GetUsageReportForAdminAsync(int? userIdFilter, int? coponIdFilter);
        Task<List<CouponUsageStoreOwnerDto>> GetUsageReportByStoreOwnerAsync(int storeOwnerUserId);
        Task<bool> ApproveCouponUsageByStoreAsync(int usageId, int storeOwnerUserId);
    }
}
