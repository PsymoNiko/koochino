using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace MyShop.Api.Middleware
{
    /// <summary>
    /// Simple in-memory rate limit for auth endpoints (.NET 6 compatible, no extra package).
    /// </summary>
    public class AuthRateLimitMiddleware
    {
        private static readonly ConcurrentDictionary<string, Counter> Counters = new();
        private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);
        private const int MaxRequests = 20;

        private readonly RequestDelegate _next;

        public AuthRateLimitMiddleware(RequestDelegate next) => _next = next;

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value ?? "";
            if (!path.StartsWith("/api/Auth", StringComparison.OrdinalIgnoreCase) &&
                !path.StartsWith("/api/auth", StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }

            var pathLower = path.ToLowerInvariant();
            var isLookup = pathLower.Contains("/find") || pathLower.Contains("/findrole");
            var max = isLookup ? 8 : MaxRequests;
            var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var key = $"{ip}:{path}";
            var now = DateTime.UtcNow;

            var entry = Counters.AddOrUpdate(
                key,
                _ => new Counter { WindowStart = now, Count = 1 },
                (_, existing) =>
                {
                    if (now - existing.WindowStart > Window)
                        return new Counter { WindowStart = now, Count = 1 };
                    existing.Count++;
                    return existing;
                });

            if (entry.Count > max)
            {
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await context.Response.WriteAsJsonAsync(new
                {
                    Success = false,
                    Message = "تعداد درخواست‌ها بیش از حد مجاز است. کمی بعد تلاش کنید."
                });
                return;
            }

            await _next(context);
        }

        sealed class Counter
        {
            public DateTime WindowStart;
            public int Count;
        }
    }
}
