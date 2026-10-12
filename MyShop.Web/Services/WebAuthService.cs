using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using MyShop.Application.Interfaces;
using MyShop.Core.Helpers;
using MyShop.Core.Interfaces;
using MyShop.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Web.Services
{
    public class WebAuthService : IAuthService
    {
        private readonly IAuthRepository _repo;
        private readonly IHttpContextAccessor _http;
        private readonly ICurrentTenant _tenant;

        public WebAuthService(IAuthRepository repo, IHttpContextAccessor http, ICurrentTenant tenant)
        {
            _repo = repo;
            _http = http;
            _tenant = tenant;
        }

        // -----------------------------------------
        // 1) ورود با موبایل / سیستم کوکی
        // -----------------------------------------
        public async Task<(bool ok, string error, string token, User user)>
            LoginWithPhoneAsync(string phone, string password, string roleRequested)
        {
            var user = await _repo.FindByPhoneAsync(phone);
            if (user == null)
                return (false, "کاربر یافت نشد.", null, null);

            // بررسی فعال بودن حساب
            if (!user.IsActive)
                return (false, "حساب شما فعال نشده است.", null, null);

            var pass = await _repo.GetPasswordInfoAsync(user.Id);

            if (string.IsNullOrEmpty(pass.Hash) || string.IsNullOrEmpty(pass.Salt))
                return (false, "اطلاعات ورود برای این کاربر یافت نشد.", null, null);

            if (!PasswordHasher.VerifyPassword(password, pass.Hash, pass.Salt))
                return (false, "رمز عبور اشتباه است.", null, null);

            var roles = await _repo.GetUserRolesAsync(user.Id);
            var rolesList = roles?.ToList() ?? new List<string>();
            if (!RoleMatches(rolesList, roleRequested))
                return (false, $"اجازه ورود ندارید. نقش‌های شما: {string.Join(", ", rolesList)}", null, null);

            try { await _repo.UpdateLastLoginAsync(user.Id); } catch { /* نادیده */ }

            return (true, null, null, user);
        }

        // -----------------------------------------
        // 1) ورود با موبایل / سیستم کوکی (با RoleID)
        // -----------------------------------------
        public async Task<(bool ok, string error, string token, User user)>
            LoginWithPhoneRoleAsync(string phone, string password, string roleRequested, int RoleID)
        {
            var user = await _repo.GetUserByPhoneAsync(phone, RoleID);
            if (user == null)
                return (false, "کاربر یافت نشد.", null, null);

            if (!user.IsActive)
                return (false, "حساب شما فعال نشده است.", null, null);

            var pass = await _repo.GetPasswordInfoAsync(user.Id);

            if (string.IsNullOrEmpty(pass.Hash) || string.IsNullOrEmpty(pass.Salt))
                return (false, "اطلاعات ورود برای این کاربر یافت نشد.", null, null);

            if (!PasswordHasher.VerifyPassword(password, pass.Hash, pass.Salt))
                return (false, "رمز عبور اشتباه است.", null, null);

            // نقش با RoleID از UserRoles تأیید شده؛ نام نقش در دیتابیس ممکن است فارسی/متفاوت باشد و نباید ورود را قطع کند
            try { await _repo.UpdateLastLoginAsync(user.Id); } catch { /* نادیده */ }

            return (true, null, null, user);
        }

        public Task EnsureUserHasRoleAsync(int userId, int roleId)
            => _repo.EnsureUserHasRoleAsync(userId, roleId);

        public Task RemoveUserRoleAsync(int userId, int roleId)
            => _repo.RemoveUserRoleAsync(userId, roleId);

        private static bool RoleMatches(List<string> roles, string requested)
        {
            if (string.IsNullOrWhiteSpace(requested)) return false;
            if (roles == null || roles.Count == 0) return false;

            var req = requested.Trim();
            var reqCompact = CompactRole(req);

            foreach (var r in roles)
            {
                if (string.IsNullOrWhiteSpace(r)) continue;
                var role = r.Trim();
                if (role.Equals(req, StringComparison.OrdinalIgnoreCase)) return true;
                if (CompactRole(role).Equals(reqCompact, StringComparison.OrdinalIgnoreCase)) return true;
                if (KnownRoleAliases.TryGetValue(reqCompact, out var aliases))
                {
                    foreach (var alias in aliases)
                    {
                        if (role.Equals(alias, StringComparison.OrdinalIgnoreCase)) return true;
                        if (CompactRole(role).Equals(CompactRole(alias), StringComparison.OrdinalIgnoreCase)) return true;
                    }
                }
            }
            return false;
        }

        static string CompactRole(string value) => value?.Replace(" ", "").Replace("_", "") ?? "";

        static readonly Dictionary<string, string[]> KnownRoleAliases = new(StringComparer.OrdinalIgnoreCase)
        {
            ["StoreOwner"] = new[] { "StoreOwner", "Store_Owner" },
            ["Customer"] = new[] { "Customer" },
            ["Admin"] = new[] { "Admin" },
            ["Vendor"] = new[] { "Vendor" },
            ["Instructor"] = new[] { "Instructor" },
            ["Student"] = new[] { "Student" }
        };

        // -----------------------------------------
        // 2) پیدا کردن کاربر با موبایل
        // -----------------------------------------
        public Task<User> FindByPhoneAsync(string phone)
            => _repo.FindByPhoneAsync(phone);
        public Task<User> GetUserByPhoneAsync(string phone, int RoleID)
           => _repo.GetUserByPhoneAsync(phone, RoleID);
        // -----------------------------------------
        // 3) نقش‌ها
        // -----------------------------------------
        public Task<IEnumerable<string>> GetRolesAsync(int userId)
            => _repo.GetUserRolesAsync(userId);

        // -----------------------------------------
        // 4) ثبت‌نام
        // -----------------------------------------
        public async Task<(bool ok, string error)> RegisterAsync(RegisterRequest request)
        {
            // ثبت‌نام عمومی: Customer(3) یا StoreOwner(2) — Admin فقط توسط ادمین لاگین‌شده
            var httpUser = _http.HttpContext?.User;
            var isAdmin = httpUser?.Identity?.IsAuthenticated == true && httpUser.IsInRole("Admin");
            if (!isAdmin)
            {
                if (request.RoleId is not (2 or 3))
                    request.RoleId = 3;
            }
            else if (request.RoleId is not (1 or 2 or 3))
            {
                request.RoleId = 3;
            }

            // SiteId فقط از Host سرور — کلاینت را نادیده بگیر
            if (_tenant.HasSite)
                request.SiteId = _tenant.SiteId;
            else
                request.SiteId = null;

            // یک شماره می‌تواند هم مشتری و هم مغازه‌دار باشد (چند نقش روی یک کاربر)
            request.PhoneNumber = NumberFormatHelper.ToIranMobile(request.PhoneNumber ?? "");
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

            // اگر SP هنوز شماره را یکتا بداند ولی نقش وجود نداشته باشد، نقش را اضافه کن
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

        // -----------------------------------------
        // 5) دریافت کاربر
        // -----------------------------------------
        public Task<User> GetUserByIdAsync(int userId)
            => _repo.LoginByIdAsync(userId);

        // -----------------------------------------
        // 5b) تعویض رمز عبور توسط ادمین
        // -----------------------------------------
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

            // رمز روی خود کاربر است، نه روی نقش — با هر نقشی بازیابی می‌شود
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

        // -----------------------------------------
        // 6) ورود با Id
        // -----------------------------------------
        public async Task<(bool ok, string error, User user)>
            LoginWithIdAsync(int id, string roleRequested)
        {
            var user = await _repo.LoginByIdAsync(id);

            if (user == null)
                return (false, "کاربر یافت نشد.", null);

            return (true, null, user);
        }

        // -----------------------------------------
        // 7) Login (فرم عمومی)
        // -----------------------------------------
        public Task<(bool ok, string error, string firstRole)>
            LoginAsync(LoginRequest request)
        {
            return Task.FromResult((false, "این متد برای Blazor Server استفاده نمی‌شود.", (string)null));
        }

        // -----------------------------------------
        // 8) خروج
        // -----------------------------------------
        public async Task LogoutAsync()
        {
            if (_http.HttpContext != null)
            {
                await _http.HttpContext.SignOutAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme
                );
            }
        }
    }
}
