using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;

namespace MyShop.Web.Extensions
{
    public static class HttpClientResilienceExtensions
    {
        public static IHttpClientBuilder AddTransientNetworkRetry(this IHttpClientBuilder builder, int retries = 2)
        {
            return builder.AddPolicyHandler(CreateRetryPolicy(retries));
        }

        private static IAsyncPolicy<HttpResponseMessage> CreateRetryPolicy(int retries)
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(
                    retries,
                    attempt => TimeSpan.FromMilliseconds(400 * attempt * attempt));
        }
    }
}
