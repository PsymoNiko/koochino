using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using MyShop.Application.Interfaces;
using MyShop.Core.Helpers;
using MyShop.Core.Interfaces;
using MyShop.Core.Models;
using MyShop.Web.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MyShop.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IAuthRepository _repo;
        private readonly IHttpContextAccessor _http;

        public AuthService(IAuthRepository repo, IHttpContextAccessor http)
        {
            _repo = repo;
            _http = http;
        }
        private readonly FrontendAuthApi _api;

        public AuthService(FrontendAuthApi api)
        {
            _api = api;
        }

        public async Task<(bool ok, string error, string token, User user)>
     LoginWithPhoneAsync(string phone, string password, string roleRequested)
        {
            var result = await _api.LoginAsync(phone, password, roleRequested);

            // تطبیق خروجی با Interface جدید
            var user = new User
            {
                Id = 0, // چون در WASM سمت کلاینت فعلاً نداریم
                Username = "Unknown"
            };

            return (result.ok, result.error, result.token, user);
        }
        public async Task<(bool ok, string error, string token, User user)>
    LoginWithPhoneRoleAsync(string phone, string password, string roleRequested,int RoleID)
        {
            var result = await _api.LoginAsync(phone, password, roleRequested);

            // تطبیق خروجی با Interface جدید
            var user = new User
            {
                Id = 0, // چون در WASM سمت کلاینت فعلاً نداریم
                Username = "Unknown"
            };

            return (result.ok, result.error, result.token, user);
        }


        public async Task<User> FindByPhoneAsync(string phone)
        {
            if (_repo != null)
                return await _repo.FindByPhoneAsync(phone);
            if (_api != null)
                return await _api.FindUserAsync(phone);
            throw new InvalidOperationException("AuthService: neither Repository nor Api is configured.");
        }

        public async Task<User> GetUserByPhoneAsync(string phone, int RoleID)
        {
            if (_repo != null)
                return await _repo.GetUserByPhoneAsync(phone, RoleID);
            if (_api != null)
                return await _api.FindUserRoleAsync(phone, RoleID);
            throw new InvalidOperationException("AuthService: neither Repository nor Api is configured.");
        }

        public async Task<IEnumerable<string>> GetRolesAsync(int userId)
        {
            if (_repo != null)
                return await _repo.GetUserRolesAsync(userId);
            if (_api != null)
                return await _api.GetRolesAsync(userId);
            throw new InvalidOperationException("AuthService: neither Repository nor Api is configured.");
        }

        public Task EnsureUserHasRoleAsync(int userId, int roleId)
            => _repo != null
                ? _repo.EnsureUserHasRoleAsync(userId, roleId)
                : Task.CompletedTask;

        public Task RemoveUserRoleAsync(int userId, int roleId)
            => _repo != null
                ? _repo.RemoveUserRoleAsync(userId, roleId)
                : Task.CompletedTask;
     
        // ------------------------------------------------------
        // 2) RegisterAsync (اتصال به SP)
        // ------------------------------------------------------
        //public async Task<(bool ok, string error)> RegisterAsync(RegisterRequest model)
        //{
        //    var (hash, salt) = PasswordHasher.HashPassword(model.Password);

        //    int result = await _repo.RegisterUserAsync(model, hash, salt);

        //    return result switch
        //    {
        //        1 => (true, null),
        //        2 => (false, "نام کاربری تکراری است"),
        //        3 => (false, "شماره موبایل تکراری است"),
        //        4 => (false, "کد ملی تکراری است"),
        //        _ => (false, "خطا در ثبت‌نام")
        //    };
        //}


        public Task<(bool ok, string error)> RegisterAsync(RegisterRequest request)
     => _api.RegisterAsync(request);

        // ------------------------------------------------------
        // 3) GetUserByIdAsync
        // ------------------------------------------------------
        public async Task<User> GetUserByIdAsync(int userId)
        {
            return await _repo.GetUserByIdAsync(userId);
        }

    
        // ------------------------------------------------------
        // 6) LoginById (مثلاً برای refresh JWT)
        // ------------------------------------------------------
        public async Task<(bool ok, string error, User user)> LoginWithIdAsync(int id, string roleRequested)
        {
            var user = await _repo.GetUserByIdAsync(id);
            if (user == null)
                return (false, "کاربر یافت نشد", null);

            var roles = (await _repo.GetUserRolesAsync(id)).ToList();

            if (!roles.Contains(roleRequested))
                return (false, "این کاربر مجوز نقش انتخاب شده را ندارد", null);

            return (true, null, user);
        }

        // ------------------------------------------------------
        // 7) Logout (برای Blazor Server)
        // ------------------------------------------------------
        public async Task LogoutAsync()
        {
            await _http.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }

        public async Task<(bool ok, string error, string firstRole)> LoginAsync(LoginRequest request)
        {
            if (request == null)
                return (false, "درخواست نامعتبر است", null);
            if (_api == null)
                return (false, "این متد فقط با اتصال به API پشتیبانی می‌شود.", null);
            var result = await LoginWithPhoneAsync(request.PhoneNumber, request.Password, request.RoleRequested);
            if (!result.ok)
                return (false, result.error, null);
            return (true, null, request.RoleRequested);
        }

        public async Task<(bool ok, string error)> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            if (userId <= 0)
                return (false, "کاربر نامعتبر است.");
            if (string.IsNullOrWhiteSpace(currentPassword))
                return (false, "رمز فعلی را وارد کنید.");
            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 8)
                return (false, "رمز عبور جدید باید حداقل ۸ کاراکتر باشد.");
            if (_repo == null)
                return (false, "امکان تغییر رمز از طریق API در این نسخه پشتیبانی نمی‌شود.");
            if (string.Equals(currentPassword, newPassword, StringComparison.Ordinal))
                return (false, "رمز جدید نباید با رمز فعلی یکسان باشد.");

            var pass = await _repo.GetPasswordInfoAsync(userId);
            if (string.IsNullOrEmpty(pass.Hash))
                return (false, "اطلاعات رمز عبور یافت نشد.");
            if (!PasswordHasher.VerifyPassword(currentPassword, pass.Hash, pass.Salt))
                return (false, "رمز فعلی نادرست است.");

            var (hash, salt) = PasswordHasher.HashPassword(newPassword);
            var updated = await _repo.SetUserPasswordAsync(userId, hash, salt);
            return updated ? (true, null) : (false, "خطا در به‌روزرسانی رمز عبور.");
        }

        /// <summary>تعویض رمز عبور کاربر توسط ادمین</summary>
        public async Task<(bool ok, string error)> SetUserPasswordByAdminAsync(int userId, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 8)
                return (false, "رمز عبور باید حداقل ۸ کاراکتر باشد.");
            if (_repo != null)
            {
                var (hash, salt) = PasswordHasher.HashPassword(newPassword);
                var updated = await _repo.SetUserPasswordAsync(userId, hash, salt);
                return updated ? (true, null) : (false, "کاربر یافت نشد یا خطا در به‌روزرسانی.");
            }
            if (_api != null)
                return (false, "امکان تغییر رمز از طریق API در این نسخه پشتیبانی نمی‌شود.");
            throw new InvalidOperationException("AuthService: neither Repository nor Api is configured.");
        }

        public async Task<(bool ok, string error)> ResetPasswordByPhoneAsync(string phone, int roleId, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 8)
                return (false, "رمز عبور باید حداقل ۸ کاراکتر باشد.");
            if (_repo == null)
                return (false, "امکان بازیابی رمز از طریق API در این نسخه پشتیبانی نمی‌شود.");

            var user = await _repo.FindByPhoneAsync(phone);
            if (user == null && roleId is 2 or 3)
                user = await _repo.GetUserByPhoneAsync(phone, roleId);
            if (user == null)
                return (false, "کاربری با این شماره یافت نشد.");
            if (!user.IsActive)
                return (false, "حساب شما فعال نشده است.");

            var (hash, salt) = PasswordHasher.HashPassword(newPassword);
            var updated = await _repo.SetUserPasswordAsync(user.Id, hash, salt);
            return updated ? (true, null) : (false, "خطا در به‌روزرسانی رمز عبور.");
        }
    }
}
