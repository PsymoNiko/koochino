using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Components.Authorization;
using MyShop.Web.Services;
using MyShop.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using MyShop.Web.Models;

namespace MyShop.Web.Pages.Account
{
    [EnableRateLimiting("login")]
    public class SignInStoreModel : PageModel
    {
        private readonly AuthenticationStateProvider _authStateProvider;
        private readonly IUserActivityService _activityService;
        private readonly IMemoryCache _cache;

        public SignInStoreModel(
            AuthenticationStateProvider authStateProvider,
            IUserActivityService activityService,
            IMemoryCache cache)
        {
            _authStateProvider = authStateProvider;
            _activityService = activityService;
            _cache = cache;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            string userId;
            string username;
            string phoneNumber;
            string firstName;
            string lastName;
            string email;
            var rememberMe = true;

            var token = Request.Query["token"].ToString();
            if (!string.IsNullOrEmpty(token))
            {
                var key = "PendingSignIn_" + token;
                if (!_cache.TryGetValue(key, out PendingSignInData? data) || data == null || data.Role != "StoreOwner")
                {
                    return RedirectToPage("/Account/login-store", new { error = "لینک ورود منقضی یا نامعتبر است." });
                }
                _cache.Remove(key);
                userId = data.UserId;
                username = data.Username ?? "";
                phoneNumber = data.PhoneNumber ?? "";
                firstName = data.FirstName ?? "";
                lastName = data.LastName ?? "";
                email = data.Email ?? "";
                rememberMe = data.RememberMe;
            }
            else
            {
                userId = HttpContext.Session.GetString("PendingSignIn_UserId") ?? "";
                if (string.IsNullOrEmpty(userId) || HttpContext.Session.GetString("PendingSignIn_Role") != "StoreOwner")
                {
                    return RedirectToPage("/Account/login-store", new { error = "خطا در ورود" });
                }
                username = HttpContext.Session.GetString("PendingSignIn_Username") ?? "";
                phoneNumber = HttpContext.Session.GetString("PendingSignIn_PhoneNumber") ?? "";
                firstName = HttpContext.Session.GetString("PendingSignIn_FirstName") ?? "";
                lastName = HttpContext.Session.GetString("PendingSignIn_LastName") ?? "";
                email = HttpContext.Session.GetString("PendingSignIn_Email") ?? "";

                HttpContext.Session.Remove("PendingSignIn_UserId");
                HttpContext.Session.Remove("PendingSignIn_Role");
                HttpContext.Session.Remove("PendingSignIn_Username");
                HttpContext.Session.Remove("PendingSignIn_PhoneNumber");
                HttpContext.Session.Remove("PendingSignIn_FirstName");
                HttpContext.Session.Remove("PendingSignIn_LastName");
                HttpContext.Session.Remove("PendingSignIn_Email");
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.MobilePhone, phoneNumber),
                new Claim(ClaimTypes.Role, "StoreOwner")
            };

            if (!string.IsNullOrEmpty(firstName))
                claims.Add(new Claim(ClaimTypes.GivenName, firstName));

            if (!string.IsNullOrEmpty(lastName))
                claims.Add(new Claim(ClaimTypes.Surname, lastName));

            if (!string.IsNullOrEmpty(email))
                claims.Add(new Claim(ClaimTypes.Email, email));

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    AllowRefresh = true,
                    ExpiresUtc = DateTime.UtcNow.AddDays(rememberMe ? 30 : 1)
                });

            // اطلاع دادن به AuthenticationStateProvider که state تغییر کرده
            if (_authStateProvider is CustomAuthStateProvider customAuth)
            {
                customAuth.StateChanged();
            }

            // ثبت لاگین در Activity Log
            try
            {
                if (int.TryParse(userId, out int userIdInt))
                {
                    var sessionId = HttpContext.Session?.Id;
                    var ipAddress = HttpContext.Connection?.RemoteIpAddress?.ToString();
                    var userAgent = HttpContext.Request?.Headers["User-Agent"].ToString();
                    
                    await _activityService.LogLoginAsync(
                        userIdInt,
                        sessionId,
                        null, // ConnectionId بعداً در SignalR Hub تنظیم می‌شود
                        ipAddress,
                        userAgent
                    );
                }
            }
            catch (Exception ex)
            {
                // Log error but don't block login
                System.Diagnostics.Debug.WriteLine($"Error logging activity: {ex.Message}");
            }

            return Redirect("/storeowner/dashboard");
        }
    }
}
