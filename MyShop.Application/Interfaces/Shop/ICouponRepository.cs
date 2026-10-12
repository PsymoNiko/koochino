using System.Collections.Generic;
using MyShop.Core.Models;
using MyShop.Core.Models.Dto;
using System.Threading.Tasks;

namespace MyShop.Application.Interfaces
{
    public interface ICouponRepository
    {
        Task<List<Coupon>> GetByUserIdAsync(int userId);
        Task<List<Coupon>> GetByStoreIdAsync(int storeId);
        Task<List<Coupon>> GetAllPendingAsync();
        Task<List<Coupon>> GetAllActiveAsync();
        Task<List<Coupon>> GetAllAsync();
        Task<Coupon> GetByIdAsync(int couponId);
        Task<int> CreateAsync(Coupon coupon);
        Task<bool> UpdateAsync(Coupon coupon);
        Task<bool> DeleteAsync(int couponId);
        Task<bool> ApproveByAdminAsync(int couponId, bool approved);
        Task<bool> ToggleStoreStatusAsync(int couponId, bool status);
        Task<CouponValidateResultDto> ValidateCouponAsync(int userId, string couponCode, long purchaseAmount);
        Task<(int Result, int NewId)> RecordCouponUsageAsync(int coponId, int userId, long purchaseAmount, long discountAmount);
        Task SetCouponUsageOrderIdAsync(int couponUsageId, int orderId);
        Task SetCouponUsageRedeemStoreAsync(int couponUsageId, int redeemStoreId, long? reservedAmount);
        Task<List<CouponUsage>> GetUsagesByUserIdAsync(int userId);
        Task<List<CouponUsageReportDto>> GetUsageReportForAdminAsync(int? userIdFilter, int? coponIdFilter);
        Task<List<CouponUsageStoreOwnerDto>> GetUsageReportByStoreOwnerAsync(int storeOwnerUserId);
        Task<bool> ApproveCouponUsageByStoreAsync(int usageId, int storeOwnerUserId);
        /// <summary>شناسه فروشگاه‌هایی که حداقل یک کوپن فعال (تایید ادمین و فروشگاه) دارند.</summary>
        Task<List<int>> GetStoreIdsHavingCouponAsync();
    }
}
