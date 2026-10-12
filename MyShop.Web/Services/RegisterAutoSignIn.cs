using Microsoft.Extensions.Caching.Memory;
using MyShop.Application.Interfaces;
using MyShop.Core.Models;
using MyShop.Web.Models;
using System;
using System.Threading.Tasks;

namespace MyShop.Web.Services
{
    /// <summary>
    /// بعد از ثبت‌نام موفق، بدون برگشت به صفحه لاگین، cookie sign-in را آماده می‌کند.
    /// </summary>
    public static class RegisterAutoSignIn
    {
        public static string RoleNameFromId(int roleId) => roleId switch
        {
            1 => "Admin",
            2 => "StoreOwner",
            _ => "Customer"
        };

        public static string SignInPath(int roleId) => roleId switch
        {
            2 => "/Account/SignInStore",
            1 => "/Account/SignInAdmin",
            _ => "/Account/SignInCustomer"
        };

        /// <summary>
        /// فقط پس از تأیید رمز عبور، token یک‌بارمصرف PendingSignIn می‌سازد.
        /// </summary>
        public static async Task<(string redirectUrl, string error)> TryPrepareAsync(
            IAuthService auth,
            IMemoryCache cache,
            string phone,
            string password,
            int roleId)
        {
            phone = NumberFormatHelper.NormalizePhoneDigits(phone ?? "");
            if (string.IsNullOrWhiteSpace(phone) || string.IsNullOrEmpty(password))
                return (null, "اطلاعات ورود ناقص است.");

            var roleName = RoleNameFromId(roleId);
            User user = null;

            try
            {
                var login = await auth.LoginWithPhoneRoleAsync(phone, password, roleName, roleId);
                if (login.ok && login.user != null)
                    user = login.user;
            }
            catch { /* try name-based login */ }

            if (user == null)
            {
                try
                {
                    var login = await auth.LoginWithPhoneAsync(phone, password, roleName);
                    if (login.ok && login.user != null)
                        user = login.user;
                }
                catch { /* fail below */ }
            }

            // بدون تأیید رمز، PendingSignIn ساخته نمی‌شود (جلوگیری از escalation)
            if (user == null || user.Id <= 0)
                return (null, "ثبت‌نام انجام شد، اما ورود خودکار ممکن نشد. لطفاً وارد شوید.");

            var token = Guid.NewGuid().ToString("N");
            cache.Set("PendingSignIn_" + token, new PendingSignInData
            {
                UserId = user.Id.ToString(),
                Role = roleName,
                Username = user.Username ?? "",
                PhoneNumber = user.PhoneNumber ?? phone,
                FirstName = user.FirstName ?? "",
                LastName = user.LastName ?? "",
                Email = user.Email ?? ""
            }, TimeSpan.FromMinutes(5));

            return (SignInPath(roleId) + "?token=" + token, null);
        }

        public static async Task<(string redirectUrl, string error)> TryPrepareAcademyAsync(
            IAuthService auth,
            IMemoryCache cache,
            string phone,
            string password,
            string roleName)
        {
            phone = NumberFormatHelper.NormalizePhoneDigits(phone ?? "");
            if (string.IsNullOrWhiteSpace(phone) || string.IsNullOrEmpty(password))
                return (null, "اطلاعات ورود ناقص است.");
            if (string.IsNullOrWhiteSpace(roleName))
                return (null, "نقش ورود نامعتبر است.");

            User user = null;
            try
            {
                var login = await auth.LoginWithPhoneAsync(phone, password, roleName);
                if (login.ok && login.user != null)
                    user = login.user;
            }
            catch { /* fail below */ }

            if (user == null || user.Id <= 0)
                return (null, "ثبت‌نام انجام شد، اما ورود خودکار ممکن نشد. لطفاً وارد شوید.");

            var token = Guid.NewGuid().ToString("N");
            cache.Set("PendingSignIn_" + token, new PendingSignInData
            {
                UserId = user.Id.ToString(),
                Role = roleName,
                Username = user.Username ?? "",
                PhoneNumber = user.PhoneNumber ?? phone,
                FirstName = user.FirstName ?? "",
                LastName = user.LastName ?? "",
                Email = user.Email ?? "",
                RememberMe = true
            }, TimeSpan.FromMinutes(5));

            return ("/Account/SignInAcademy?token=" + token, null);
        }
    }
}
