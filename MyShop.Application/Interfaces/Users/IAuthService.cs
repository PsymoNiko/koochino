using System.Collections.Generic;
using System.Threading.Tasks;
using MyShop.Core.Models;

namespace MyShop.Application.Interfaces
{
    public interface IAuthService
    {
        Task<(bool ok, string error, string token, User user)>
            LoginWithPhoneAsync(string phone, string password, string roleRequested);
        Task<(bool ok, string error, string token, User user)>
            LoginWithPhoneRoleAsync(string phone, string password, string roleRequested,int RoleID);
        Task<User> FindByPhoneAsync(string phone);
        Task<User> GetUserByPhoneAsync(string phone,int RoleID);

        Task<IEnumerable<string>> GetRolesAsync(int userId);

        /// <summary>افزودن نقش به کاربر (مثلاً مشتری + مغازه‌دار روی یک شماره)</summary>
        Task EnsureUserHasRoleAsync(int userId, int roleId);

        /// <summary>حذف نقش از کاربر (نقش Admin از اینجا حذف نمی‌شود مگر توسط فراخوانی صریح)</summary>
        Task RemoveUserRoleAsync(int userId, int roleId);

        Task<(bool ok, string error)> RegisterAsync(RegisterRequest request);

        Task<User> GetUserByIdAsync(int userId);

        /// <summary>تعویض رمز عبور توسط خود کاربر (رمز فعلی + رمز جدید)</summary>
        Task<(bool ok, string error)> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
        Task<(bool ok, string error)> SetUserPasswordByAdminAsync(int userId, string newPassword);

        /// <summary>بازیابی رمز عبور با شماره موبایل و نقش (پس از تأیید کد پیامکی)</summary>
        Task<(bool ok, string error)> ResetPasswordByPhoneAsync(string phone, int roleId, string newPassword);

        Task<(bool ok, string error, User user)> LoginWithIdAsync(int id, string roleRequested);

        Task<(bool ok, string error, string firstRole)> LoginAsync(LoginRequest request);

        Task LogoutAsync();
    }
}
