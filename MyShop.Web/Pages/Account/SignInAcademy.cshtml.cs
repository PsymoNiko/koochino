using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;
using MyShop.Application.Interfaces;
using MyShop.Application.Interfaces.Academy;
using MyShop.Core.Models.Academy;
using MyShop.Web.Models;
using MyShop.Web.Services;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MyShop.Web.Pages.Account
{
    [EnableRateLimiting("login")]
    public class SignInAcademyModel : PageModel
    {
        private readonly AuthenticationStateProvider _authStateProvider;
        private readonly IUserActivityService _activityService;
        private readonly IMemoryCache _cache;
        private readonly IAcademyStudentService _students;
        private readonly IAcademyInstructorService _instructors;

        public SignInAcademyModel(
            AuthenticationStateProvider authStateProvider,
            IUserActivityService activityService,
            IMemoryCache cache,
            IAcademyStudentService students,
            IAcademyInstructorService instructors)
        {
            _authStateProvider = authStateProvider;
            _activityService = activityService;
            _cache = cache;
            _students = students;
            _instructors = instructors;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var token = Request.Query["token"].ToString();
            var key = "PendingSignIn_" + token;
            if (string.IsNullOrEmpty(token) || !_cache.TryGetValue(key, out PendingSignInData data) || data == null)
                return Redirect("/Account/login-academy-student?error=" + Uri.EscapeDataString("لینک ورود منقضی یا نامعتبر است."));

            _cache.Remove(key);

            var isStudent = string.Equals(data.Role, AcademyConstants.RoleStudent, StringComparison.OrdinalIgnoreCase);
            var isInstructor = string.Equals(data.Role, AcademyConstants.RoleInstructor, StringComparison.OrdinalIgnoreCase);
            if (!isStudent && !isInstructor)
                return Redirect("/Account/login-academy-student?error=" + Uri.EscapeDataString("نقش ورود نامعتبر است."));

            var loginPage = isStudent ? "/Account/login-academy-student" : "/Account/login-academy-instructor";
            if (!int.TryParse(data.UserId, out var userId) || userId <= 0)
                return Redirect(loginPage + "?error=" + Uri.EscapeDataString("خطا در اطلاعات کاربر"));

            string home;
            if (isStudent)
            {
                var student = await _students.GetByUserIdAsync(userId);
                if (student == null || !string.Equals(student.Status, AcademyConstants.StudentStatus.Active, StringComparison.OrdinalIgnoreCase))
                    return Redirect(loginPage + "?error=" + Uri.EscapeDataString("حساب دانش‌آموزی فعال نیست."));
                home = student.MustChangePassword ? "/academy/profile" : "/academy";
            }
            else
            {
                var instructor = await _instructors.GetByUserIdAsync(userId);
                if (instructor == null || !instructor.IsActive)
                    return Redirect(loginPage + "?error=" + Uri.EscapeDataString("حساب مربی فعال نیست."));
                home = instructor.MustChangePassword ? "/instructor/profile" : "/instructor";
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, data.UserId),
                new Claim(ClaimTypes.Name, data.Username ?? ""),
                new Claim(ClaimTypes.MobilePhone, data.PhoneNumber ?? ""),
                new Claim(ClaimTypes.Role, isStudent ? AcademyConstants.RoleStudent : AcademyConstants.RoleInstructor)
            };
            if (!string.IsNullOrEmpty(data.FirstName))
                claims.Add(new Claim(ClaimTypes.GivenName, data.FirstName));
            if (!string.IsNullOrEmpty(data.LastName))
                claims.Add(new Claim(ClaimTypes.Surname, data.LastName));
            if (!string.IsNullOrEmpty(data.Email))
                claims.Add(new Claim(ClaimTypes.Email, data.Email));

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    AllowRefresh = true,
                    ExpiresUtc = DateTime.UtcNow.AddDays(data.RememberMe ? 30 : 1)
                });

            if (_authStateProvider is CustomAuthStateProvider customAuth)
                customAuth.StateChanged();

            try
            {
                await _activityService.LogLoginAsync(
                    userId,
                    HttpContext.Session?.Id,
                    null,
                    HttpContext.Connection?.RemoteIpAddress?.ToString(),
                    HttpContext.Request?.Headers["User-Agent"].ToString());
            }
            catch { }

            return Redirect(home);
        }
    }
}
