using MyShop.Core.Models.Users;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Interfaces.Users
{
    public interface IUserIdentityVerificationRepository
    {
        Task<UserIdentityVerification> GetLatestByUserIdAsync(int userId);
        Task<UserIdentityVerification> GetByIdAsync(int verificationId);
        Task<List<UserIdentityVerification>> GetLatestAsync(int count, string statusFilter = null);
        Task<int> CreateAsync(UserIdentityVerification verification);
        Task<bool> ApproveAsync(int verificationId, int reviewedBy);
        Task<bool> RejectAsync(int verificationId, int reviewedBy, string rejectionReason);

        /// <summary>ذخیره/به‌روزرسانی اطلاعات فردی مرحله ۱ ویزارد (کد ملی، نام پدر، تاریخ تولد، موبایل)</summary>
        Task<int> UpsertPersonalInfoAsync(int userId, string nationalCode, string fatherName, DateTime? birthDate, string phoneNumber);

        /// <summary>ثبت فروشگاه ساخته‌شده در مرحله ۲ روی رکورد پیشرفت کاربر</summary>
        Task AttachStoreAsync(int verificationId, int storeId);
    }
}
