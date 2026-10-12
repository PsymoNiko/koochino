using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;

namespace MyShop.Web.Services
{
    public class CustomAuthStateProvider : AuthenticationStateProvider
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CustomAuthStateProvider(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            try
            {
                var httpContext = _httpContextAccessor?.HttpContext;
                if (httpContext == null)
                    return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));

                var user = httpContext.User;
                if (user?.Identity?.IsAuthenticated == true)
                {
                    var claimsPrincipal = new ClaimsPrincipal(user);
                    return Task.FromResult(new AuthenticationState(claimsPrincipal));
                }
            }
            catch
            {
                // در صورت هر خطا (مثلاً قبل از برقراری circuit) کاربر مهمان در نظر گرفته می‌شود
            }

            return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
        }

        // این متد اختیاری است - برای اطلاع‌رسانی دستی از تغییرات
        public void StateChanged()
        {
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }
    }
}