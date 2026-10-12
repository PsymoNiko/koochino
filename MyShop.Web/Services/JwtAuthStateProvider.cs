using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MyShop.Web.Services
{
    public class JwtAuthStateProvider : AuthenticationStateProvider
    {
        private readonly TokenStorageService _tokenStorage;
        private readonly JwtTokenValidator _validator;

        public JwtAuthStateProvider(TokenStorageService tokenStorage, JwtTokenValidator validator)
        {
            _tokenStorage = tokenStorage;
            _validator = validator;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            ClaimsPrincipal principal;

            try
            {
                var token = await _tokenStorage.GetTokenAsync();

                if (!string.IsNullOrEmpty(token) && _validator.TryValidate(token, out var validated))
                {
                    principal = validated;
                }
                else if (!string.IsNullOrEmpty(token))
                {
                    // Invalid or expired token — treat as anonymous (fail-closed for JWT path).
                    await _tokenStorage.RemoveTokenAsync();
                    principal = new ClaimsPrincipal(new ClaimsIdentity());
                }
                else
                {
                    principal = new ClaimsPrincipal(new ClaimsIdentity());
                }
            }
            catch
            {
                principal = new ClaimsPrincipal(new ClaimsIdentity());
            }

            return new AuthenticationState(principal);
        }

        public void NotifyUserAuthentication()
        {
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }

        public void NotifyUserLogout()
        {
            var anon = new ClaimsPrincipal(new ClaimsIdentity());
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(anon)));
        }
        
        public async Task LogoutAsync()
        {
            await _tokenStorage.RemoveTokenAsync();

            var anonymous = new ClaimsPrincipal(new ClaimsIdentity());
            NotifyAuthenticationStateChanged(
                Task.FromResult(new AuthenticationState(anonymous))
            );
        }

    }
}
