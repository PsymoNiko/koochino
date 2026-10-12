using MyShop.Core.Models.Users;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Interfaces.Users
{
    public interface IUserIdentityVerificationService
    {
        Task<UserIdentityVerification> GetLatestByUserIdAsync(int userId);
        Task<UserIdentityVerification> GetByIdAsync(int verificationId);
        Task<List<UserIdentityVerification>> GetLatestAsync(int count, string statusFilter = null);

        /// <summary>یک کد احراز هویت تصادفی جدید تولید می‌کند (برای نمایش در مرحله سلفی)</summary>
        string GenerateVerificationCode();

        Task<UserIdentityVerification> SubmitAsync(
            int userId,
            string nationalCode,
            string fatherName,
            DateTime? birthDate,
            string phoneNumber,
            string nationalCardImageUrl,
            string selfieImageUrl,
            string verificationCode);

        Task<bool> ApproveAsync(int verificationId, int reviewedBy);
        Task<bool> RejectAsync(int verificationId, int reviewedBy, string rejectionReason);

        /// <summary>رکورد پیشرفت ثبت‌نام کاربر (اطلاعات فردی + StoreId) را برمی‌گرداند؛ اگر هنوز شروع نکرده null است</summary>
        Task<UserIdentityVerification> GetRegistrationProgressAsync(int userId);

        /// <summary>ذخیره مرحله ۱ ویزارد: اطلاعات فردی + همگام‌سازی نام/نام‌خانوادگی/کد ملی روی حساب کاربری</summary>
        Task<UserIdentityVerification> SaveStep1Async(
            int userId,
            string nationalCode,
            string firstName,
            string lastName,
            string fatherName,
            DateTime? birthDate);

        /// <summary>ثبت فروشگاه ساخته‌شده در مرحله ۲ روی رکورد پیشرفت کاربر</summary>
        Task AttachStoreAsync(int verificationId, int storeId);
    }
}
