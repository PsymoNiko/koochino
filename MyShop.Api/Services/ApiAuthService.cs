using MyShop.Api.Auth;
using MyShop.Application.Interfaces;
using MyShop.Core.Helpers;
using MyShop.Core.Interfaces;
using MyShop.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Api.Services
{
    public class ApiAuthService : IAuthService
    {
        private readonly IAuthRepository _repo;
        private readonly ITokenService _tokenService;
        private readonly ICurrentTenant _tenant;

        public ApiAuthService(IAuthRepository repo, ITokenService tokenService, ICurrentTenant tenant)
        {
            _repo = repo;
            _tokenService = tokenService;
            _tenant = tenant;
        }

        public async Task<(bool ok, string error, string token, User user)>
            LoginWithPhoneAsync(string phone, string password, string roleRequested)
        {
            // 1) کاربر را پیدا کن
            var user = await _repo.FindByPhoneAsync(phone);
            if (user == null)
                return (false, "کاربر یافت نشد", null, null);

            // 2) اعتبارسنجی رمز
            var pass = await _repo.GetPasswordInfoAsync(user.Id);

            if (string.IsNullOrEmpty(pass.Hash) || string.IsNullOrEmpty(pass.Salt))
                return (false, "اطلاعات ورود برای این کاربر یافت نشد", null, null);

            if (!MyShop.Core.Helpers.PasswordHasher.VerifyPassword(password, pass.Hash, pass.Salt))
                return (false, "رمز اشتباه است", null, null);

            // 3) نقش‌ها
            var roles = await _repo.GetUserRolesAsync(user.Id);

            if (!roles.Contains(roleRequested))
                return (false, "اجازه ورود ندارید", null, null);

            // 4) ساخت JWT (اعتبار ۲ ساعته)
            string token = _tokenService.GenerateToken(
                userId: user.Id,
                username: user.Username,
                role: roleRequested,
                expiresMinutes: 120
            );

            return (true, null, token, user);
        }
        public async Task<(bool ok, string error, string token, User user)>
             LoginWithPhoneRoleAsync(string phone, string password, string roleRequested,int RoleID)
        {
            // 1) کاربر را پیدا کن
            var user = await _repo.GetUserByPhoneAsync(phone, RoleID);
            if (user == null)
                return (false, "کاربر یافت نشد", null, null);

            // 2) اعتبارسنجی رمز
            var pass = await _repo.GetPasswordInfoAsync(user.Id);

            if (string.IsNullOrEmpty(pass.Hash) || string.IsNullOrEmpty(pass.Salt))
                return (false, "اطلاعات ورود برای این کاربر یافت نشد", null, null);

            if (!MyShop.Core.Helpers.PasswordHasher.VerifyPassword(password, pass.Hash, pass.Salt))
                return (false, "رمز اشتباه است", null, null);

            // نقش با RoleID از UserRoles تأیید شده

            // 4) ساخت JWT (اعتبار ۲ ساعته)
            string token = _tokenService.GenerateToken(
                userId: user.Id,
                username: user.Username,
                role: roleRequested,
                expiresMinutes: 120
            );

            return (true, null, token, user);
        }

        public Task EnsureUserHasRoleAsync(int userId, int roleId)
            => _repo.EnsureUserHasRoleAsync(userId, roleId);

        public Task RemoveUserRoleAsync(int userId, int roleId)
            => _repo.RemoveUserRoleAsync(userId, roleId);

        public async Task<(bool ok, string error, string token, User user)>

                  GetUserByPhoneAsync(string phone, string password, string roleRequested,int RoleID)
        {
            // 1) کاربر را پیدا کن
            var user = await _repo.GetUserByPhoneAsync(phone,RoleID);
            if (user == null)
                return (false, "کاربر یافت نشد", null, null);

            // 2) اعتبارسنجی رمز
            var pass = await _repo.GetPasswordInfoAsync(user.Id);

            if (!MyShop.Core.Helpers.PasswordHasher.VerifyPassword(password, pass.Hash, pass.Salt))
                return (false, "رمز اشتباه است", null, null);

            // 3) نقش‌ها
            var roles = await _repo.GetUserRolesAsync(user.Id);

            if (!roles.Contains(roleRequested))
                return (false, "اجازه ورود ندارید", null, null);

            // 4) ساخت JWT (اعتبار ۲ ساعته)
            string token = _tokenService.GenerateToken(
                userId: user.Id,
                username: user.Username,
                role: roleRequested,
                expiresMinutes: 120
            );

            return (true, null, token, user);
        }

        public async Task<(bool ok, string error)> RegisterAsync(RegisterRequest request)
        {
            // API عمومی: Customer(3) یا StoreOwner(2)
            if (request.RoleId is not (2 or 3))
                request.RoleId = 3;

            if (_tenant != null && _tenant.HasSite)
                request.SiteId = _tenant.SiteId;
            else
                request.SiteId = null;

            // یک شماره می‌تواند هم مشتری و هم مغازه‌دار باشد
            request.PhoneNumber = NormalizePhone(request.PhoneNumber);
            if (string.IsNullOrWhiteSpace(request.PhoneNumber) || request.PhoneNumber.Length < 11)
                return (false, "شماره موبایل معتبر نیست.");

            var existing = await _repo.FindByPhoneAsync(request.PhoneNumber);
            if (existing != null)
            {
                var alreadyHasRole = await _repo.GetUserByPhoneAsync(request.PhoneNumber, request.RoleId);
                if (alreadyHasRole != null)
                    return (false, "این نقش قبلاً برای این شماره ثبت شده است");

                if (!existing.IsActive)
                    return (false, "حساب شما فعال نشده است.");

                if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
                    return (false, "رمز عبور باید حداقل ۸ کاراکتر باشد.");

                var pass = await _repo.GetPasswordInfoAsync(existing.Id);
                if (string.IsNullOrEmpty(pass.Hash) || string.IsNullOrEmpty(pass.Salt)
                    || !PasswordHasher.VerifyPassword(request.Password, pass.Hash, pass.Salt))
                {
                    return (false, "این شماره قبلاً ثبت شده است. برای افزودن نقش جدید، رمز عبور فعلی حساب را وارد کنید.");
                }

                await _repo.EnsureUserHasRoleAsync(existing.Id, request.RoleId);
                return (true, null);
            }

            var (hash, salt) = PasswordHasher.HashPassword(request.Password);

            int result = await _repo.RegisterUserAsync(request, hash, salt);

            if (result == 3)
            {
                var again = await _repo.FindByPhoneAsync(request.PhoneNumber);
                if (again != null)
                {
                    var alreadyHasRole = await _repo.GetUserByPhoneAsync(request.PhoneNumber, request.RoleId);
                    if (alreadyHasRole != null)
                        return (false, "این نقش قبلاً برای این شماره ثبت شده است");

                    var pass = await _repo.GetPasswordInfoAsync(again.Id);
                    if (!string.IsNullOrEmpty(pass.Hash) && !string.IsNullOrEmpty(pass.Salt)
                        && PasswordHasher.VerifyPassword(request.Password, pass.Hash, pass.Salt))
                    {
                        await _repo.EnsureUserHasRoleAsync(again.Id, request.RoleId);
                        return (true, null);
                    }

                    return (false, "این شماره قبلاً ثبت شده است. برای افزودن نقش جدید، رمز عبور فعلی حساب را وارد کنید.");
                }
            }

            return result switch
            {
                1 => (true, null),
                2 => (false, "نام کاربری تکراری است"),
                3 => (false, "شماره موبایل تکراری است"),
                4 => (false, "کد ملی تکراری است"),
                5 => (false, "این نقش قبلاً برای این شماره ثبت شده است"),
                _ => (false, "خطا در ثبت‌نام")
            };
        }

        public Task<User> FindByPhoneAsync(string phone)
            => _repo.FindByPhoneAsync(phone);
        public Task<User> GetUserByPhoneAsync(string phone, int RoleID)
         => _repo.GetUserByPhoneAsync(phone, RoleID);

        public Task<IEnumerable<string>> GetRolesAsync(int userId)
            => _repo.GetUserRolesAsync(userId);

   
        public Task<User> GetUserByIdAsync(int userId)
            => _repo.GetUserByIdAsync(userId);

        public Task<(bool ok, string error, User user)> LoginWithIdAsync(int id, string roleRequested)
           => Task.FromResult((false, "Not supported.", (User)null));

        public Task<(bool ok, string error, string firstRole)> LoginAsync(LoginRequest request)
            => Task.FromResult((false, "Not supported.", (string)null));

        public Task LogoutAsync()
            => Task.CompletedTask;

        public async Task<(bool ok, string error)> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            if (userId <= 0)
                return (false, "کاربر نامعتبر است.");
            if (string.IsNullOrWhiteSpace(currentPassword))
                return (false, "رمز فعلی را وارد کنید.");
            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 8)
                return (false, "رمز عبور جدید باید حداقل ۸ کاراکتر باشد.");
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

        public async Task<(bool ok, string error)> SetUserPasswordByAdminAsync(int userId, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 8)
                return (false, "رمز عبور باید حداقل ۸ کاراکتر باشد.");
            var (hash, salt) = PasswordHasher.HashPassword(newPassword);
            var updated = await _repo.SetUserPasswordAsync(userId, hash, salt);
            return updated ? (true, null) : (false, "کاربر یافت نشد یا خطا در به‌روزرسانی.");
        }

        public async Task<(bool ok, string error)> ResetPasswordByPhoneAsync(string phone, int roleId, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 8)
                return (false, "رمز عبور باید حداقل ۸ کاراکتر باشد.");

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

        private static string NormalizePhone(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "";
            var sb = new System.Text.StringBuilder(input.Length);
            foreach (var c in input.Trim())
            {
                if (c >= '0' && c <= '9') sb.Append(c);
                else if (c >= '۰' && c <= '۹') sb.Append((char)('0' + (c - '۰')));
                else if (c >= '٠' && c <= '٩') sb.Append((char)('0' + (c - '٠')));
            }
            var value = sb.ToString();
            if (value.StartsWith("98") && value.Length >= 12) value = "0" + value.Substring(2);
            if (value.StartsWith("9") && value.Length == 10) value = "0" + value;
            return value;
        }
    }
}
