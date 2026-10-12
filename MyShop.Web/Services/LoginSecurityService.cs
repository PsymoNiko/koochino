using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.Security.Cryptography;

namespace MyShop.Web.Services
{
    public class LoginSecurityService : ILoginSecurityService
    {
        private const int FailedAttemptsBeforeCaptcha = 2;
        private const int FailedAttemptsBeforeBlock = 15;
        private static readonly TimeSpan AttemptWindow = TimeSpan.FromMinutes(15);
        private const string AttemptCacheKeyPrefix = "LoginAttempts:";
        private const string CaptchaCacheKeyPrefix = "Captcha:";
        private static readonly TimeSpan CaptchaExpiry = TimeSpan.FromMinutes(5);

        private readonly IMemoryCache _cache;
        private readonly IHttpContextAccessor _http;

        public LoginSecurityService(IMemoryCache cache, IHttpContextAccessor http)
        {
            _cache = cache;
            _http = http;
        }

        private string GetClientKey(string loginType)
        {
            var ip = _http.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "unknown";
            var forwarded = _http.HttpContext?.Request?.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwarded))
                ip = forwarded.Split(',')[0].Trim();
            return $"{AttemptCacheKeyPrefix}{loginType}:{ip}";
        }

        public bool IsTemporarilyBlocked(string loginType)
        {
            var key = GetClientKey(loginType);
            if (!_cache.TryGetValue(key, out AttemptRecord record) || record == null)
                return false;
            if (DateTime.UtcNow - record.FirstAttemptUtc > AttemptWindow)
                return false;
            return record.FailedCount >= FailedAttemptsBeforeBlock;
        }

        public bool RequiresCaptcha(string loginType)
        {
            var key = GetClientKey(loginType);
            if (!_cache.TryGetValue(key, out AttemptRecord record) || record == null)
                return false;
            if (DateTime.UtcNow - record.FirstAttemptUtc > AttemptWindow)
                return false;
            return record.FailedCount >= FailedAttemptsBeforeCaptcha;
        }

        public void RecordFailedLogin(string loginType)
        {
            var key = GetClientKey(loginType);
            if (!_cache.TryGetValue(key, out AttemptRecord record) || record == null)
                record = new AttemptRecord { FailedCount = 0, FirstAttemptUtc = DateTime.UtcNow };
            record.FailedCount++;
            _cache.Set(key, record, AttemptWindow);
        }

        public void ClearFailedLogins(string loginType)
        {
            var key = GetClientKey(loginType);
            _cache.Remove(key);
        }

        public (string questionText, string captchaId) GenerateCaptcha()
        {
            int a = RandomNumberGenerator.GetInt32(1, 15);
            int b = RandomNumberGenerator.GetInt32(1, 15);
            int answer = a + b;
            var captchaId = Guid.NewGuid().ToString("N");
            var cacheKey = CaptchaCacheKeyPrefix + captchaId;
            _cache.Set(cacheKey, answer, CaptchaExpiry);
            return ($"{a} + {b} = ?", captchaId);
        }

        public bool ValidateCaptcha(string captchaId, string userAnswer)
        {
            if (string.IsNullOrWhiteSpace(captchaId) || string.IsNullOrWhiteSpace(userAnswer))
                return false;
            var cacheKey = CaptchaCacheKeyPrefix + captchaId;
            if (!_cache.TryGetValue(cacheKey, out int expected))
                return false;
            _cache.Remove(cacheKey);
            return int.TryParse(userAnswer.Trim(), out int entered) && entered == expected;
        }

        private class AttemptRecord
        {
            public int FailedCount { get; set; }
            public DateTime FirstAttemptUtc { get; set; }
        }
    }
}
