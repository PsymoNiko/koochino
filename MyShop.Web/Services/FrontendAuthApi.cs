using MyShop.Core.Models;
using MyShop.Core.Models.Dto;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Numerics;
using System.Threading.Tasks;

namespace MyShop.Web.Services
{
    public class FrontendAuthApi
    {
        private readonly HttpClient _http;

        public FrontendAuthApi(HttpClient http)
        {
            _http = http;
        }

        // -------------------------
        // REGISTER
        // -------------------------
        public async Task<(bool ok, string error)> RegisterAsync(RegisterRequest model)
        {
            var response = await _http.PostAsJsonAsync("api/auth/register", model);

            if (!response.IsSuccessStatusCode)
                return (false, "خطا در ارتباط با سرور");

            var result = await response.Content.ReadFromJsonAsync<ApiRegisterResponse>();
            return (result.Success, result.Message);
        }



        // -------------------------
        // FIND USER
        // -------------------------
        public async Task<User> FindUserAsync(string phone)
        {
            try
            {
                var response = await _http.GetAsync($"api/auth/find/{Uri.EscapeDataString(phone ?? "")}");

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return null;

                if (!response.IsSuccessStatusCode)
                    return null;

                var result = await response.Content.ReadFromJsonAsync<ApiFindUserResponse>();
                return result?.Success == true ? MapLookup(result.User) : null;
            }
            catch
            {
                return null;
            }
        }
        // -------------------------
        // FIND USER by role
        // -------------------------
        public async Task<User> FindUserRoleAsync(string phone, int roleId)
        {
            try
            {
                var response = await _http.GetAsync(
                    $"api/auth/findrole?phone={Uri.EscapeDataString(phone ?? "")}&roleId={roleId}"
                );

                if (!response.IsSuccessStatusCode)
                    return null;

                var result = await response.Content
                    .ReadFromJsonAsync<ApiFindUserResponse>();

                return result?.Success == true ? MapLookup(result.User) : null;
            }
            catch
            {
                return null;
            }
        }

        static User MapLookup(ApiLookupUser lookup)
        {
            if (lookup == null || lookup.Id <= 0)
                return null;
            return new User
            {
                Id = lookup.Id,
                IsActive = lookup.IsActive,
                RoleId = lookup.RoleId,
                RoleName = lookup.RoleName
            };
        }

        public async Task<IEnumerable<string>> GetRolesAsync(int userId)
        {
            return await _http.GetFromJsonAsync<IEnumerable<string>>(
                $"api/auth/roles/{userId}"
            );
        }
        // -------------------------
        // PROFILE (برای UserProfile.razor)
        // -------------------------


        public async Task<(bool success, string error)> UpdateProfileAsync(UpdateUserProfileDto dto)
        {
            var response = await _http.PostAsJsonAsync("api/user/profile", dto);

            if (!response.IsSuccessStatusCode)
                return (false, "خطا در ذخیره اطلاعات");

            return (true, null);
        }



        // ======================
        // USER PROFILE
        // ======================
        //public async Task<UserProfileVm> GetProfileAsync()
        //{
        //    try
        //    {
        //        var response = await _http.GetAsync($"api/user/profile");

        //        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        //            return null;

        //        if (!response.IsSuccessStatusCode)
        //            return null;

        //        var result = await response.Content.ReadFromJsonAsync<ApiUserProfileVmResponse>();
        //        return result?.Success == true ? result.User : null;
        //    }
        //    catch
        //    {
        //        return null;
        //    }

          
        //}


        public async Task<UserProfileVm> GetProfileAsync()
        {
            try
            {
                var res = await _http.GetAsync("api/user/profile");

                if (res.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    // کاربر لاگین نیست یا توکن مشکل دارد
                    return null;
                }

                if (res.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    // پروفایل پیدا نشد
                    return null;
                }

                if (!res.IsSuccessStatusCode)
                {
                    var body = await res.Content.ReadAsStringAsync();
                    throw new Exception($"Profile API Error: {(int)res.StatusCode} - {body}");
                }

                var profile = await res.Content.ReadFromJsonAsync<UserProfileVm>();
                return profile;
            }
            catch (HttpRequestException ex)
            {
                // خطا در ارتباط با سرور
                throw new Exception($"خطا در ارتباط با سرور: {ex.Message}", ex);
            }
            catch (TaskCanceledException)
            {
                // timeout
                throw new Exception("زمان درخواست به پایان رسید");
            }
        }
        public async Task<(bool ok, string error)> UpdateUserProfileAsync(UpdateUserProfileDto dto)
        {
            var response = await _http.PostAsJsonAsync("api/user/profile", dto);

            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync();
                return (false, err);
            }

            return (true, null);
        }

        // ======================
        // LOGIN
        // ======================
        public async Task<(bool ok, string error, string token, string role)>
            LoginAsync(string phone, string password, string roleRequested)
        {
            var req = new
            {
                PhoneNumber = phone,
                Password = password,
                RoleRequested = roleRequested
            };

            var response = await _http.PostAsJsonAsync("api/auth/login", req);

            if (!response.IsSuccessStatusCode)
                return (false, "خطا در ارتباط با سرور", null, null);

            var data = await response.Content.ReadFromJsonAsync<ApiLoginResponse>();

            if (data == null || !data.Success)
                return (false, data?.Message ?? "ورود ناموفق", null, null);

            return (true, null, data.Token, data.Role);
        }



        // -------------------------
        // DTOs
        // -------------------------
        private class ApiLoginResponse
        {
            public bool Success { get; set; }
            public string Message { get; set; }
            public string Token { get; set; }
            public string Role { get; set; }
        }



        private class ApiRegisterResponse
        {
            public bool Success { get; set; }
            public string Message { get; set; }
        }

        private class ApiFindUserResponse
        {
            public bool Success { get; set; }
            public bool Exists { get; set; }
            public ApiLookupUser User { get; set; }
        }

        private class ApiLookupUser
        {
            public int Id { get; set; }
            public bool IsActive { get; set; }
            public int RoleId { get; set; }
            public string RoleName { get; set; }
        }
        private class ApiUserProfileVmResponse
        {
            public bool Success { get; set; }
            public UserProfileVm User { get; set; }
        }
    }
}
