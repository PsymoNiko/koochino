using MyShop.Application.Interfaces;
using MyShop.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Web.Services
{
    public class JwtAuthService : IAuthService
    {
        private readonly FrontendAuthApi _api;

        public JwtAuthService(FrontendAuthApi api)
        {
            _api = api;
        }

        // -------------------------
        // LOGIN
        // -------------------------
        public async Task<(bool ok, string error, string token, User user)>
            LoginWithPhoneAsync(string phone, string password, string roleRequested)
        {
            var result = await _api.LoginAsync(phone, password, roleRequested);

            // Blazor Web سمت — فقط برای تطبیق Interface
            var user = new User
            {
                Id = 0,
                Username = "Unknown"
            };

            return (result.ok, result.error, result.token, user);
        }
        public async Task<(bool ok, string error, string token, User user)>
          LoginWithPhoneRoleAsync(string phone, string password, string roleRequested,int RoleID)
        {
            var result = await _api.LoginAsync(phone, password, roleRequested);

            // Blazor Web سمت — فقط برای تطبیق Interface
            var user = new User
            {
                Id = 0,
                Username = "Unknown"
            };

            return (result.ok, result.error, result.token, user);
        }


        // -------------------------
        // FIND USER
        // -------------------------
        public Task<User> FindByPhoneAsync(string phone)
            => _api.FindUserAsync(phone);
        public Task<User> GetUserByPhoneAsync(string phone, int RoleID)
           => _api.FindUserRoleAsync(phone, RoleID);

        // -------------------------
        // GET ROLES
        // -------------------------
        public Task<IEnumerable<string>> GetRolesAsync(int userId)
            => _api.GetRolesAsync(userId);

        public Task EnsureUserHasRoleAsync(int userId, int roleId)
            => Task.CompletedTask;

        public Task RemoveUserRoleAsync(int userId, int roleId)
            => Task.CompletedTask;

        // -------------------------
        // ✅ REGISTER (اصلاح شده)
        // -------------------------
        public async Task<(bool ok, string error)> RegisterAsync(RegisterRequest request)
        {
            return await _api.RegisterAsync(request);
        }


        // -------------------------
        // NOT USED (برای آینده)
        // -------------------------
        public Task<User> GetUserByIdAsync(int userId)
            => Task.FromResult<User>(null);

        public Task<(bool ok, string error, User user)> LoginWithIdAsync(int id, string roleRequested)
            => Task.FromResult((false, "Not supported.", (User)null));

        public Task<(bool ok, string error, string firstRole)> LoginAsync(LoginRequest request)
            => Task.FromResult((false, "Not supported.", (string)null));

        public Task LogoutAsync()
            => Task.CompletedTask;

        public Task<(bool ok, string error)> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
            => Task.FromResult((false, "امکان تغییر رمز از این طریق پشتیبانی نمی‌شود."));

        public Task<(bool ok, string error)> SetUserPasswordByAdminAsync(int userId, string newPassword)
            => Task.FromResult((false, "امکان تغییر رمز از این طریق پشتیبانی نمی‌شود. از حالت اتصال مستقیم به دیتابیس استفاده کنید."));

        public Task<(bool ok, string error)> ResetPasswordByPhoneAsync(string phone, int roleId, string newPassword)
            => Task.FromResult((false, "امکان بازیابی رمز از این طریق پشتیبانی نمی‌شود."));

    }
}
