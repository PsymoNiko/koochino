using MyShop.Web.Services;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace MyShop.Web.Services
{
    public class JwtAuthorizationMessageHandler : DelegatingHandler
    {
        private readonly TokenStorageService _tokenStorage;

        public JwtAuthorizationMessageHandler(TokenStorageService tokenStorage)
        {
            _tokenStorage = tokenStorage;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            try
            {
                var token = await _tokenStorage.GetTokenAsync();

                if (!string.IsNullOrWhiteSpace(token))
                {
                    request.Headers.Authorization =
                        new AuthenticationHeaderValue("Bearer", token);
                }
            }
            catch (Exception)
            {
                // در صورت خطا، بدون توکن ادامه بده
                // این می‌تواند در زمان prerendering اتفاق بیفتد
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
