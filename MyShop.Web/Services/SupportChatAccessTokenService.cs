using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace MyShop.Web.Services
{
    /// <summary>توکن دسترسی چت مهمان: محدود به chatId+siteId و دارای انقضا. کوکی HttpOnly بدون تغییر UI.</summary>
    public class SupportChatAccessTokenService
    {
        public const string CookieName = ".Koch.ScAccess";
        public static readonly TimeSpan Lifetime = TimeSpan.FromHours(24);

        private readonly IHttpContextAccessor _http;
        private readonly IDataProtector _protector;
        private readonly byte[] _hmacKey;
        private readonly IHostEnvironment _env;
        string _lastSealed;

        public SupportChatAccessTokenService(
            IHttpContextAccessor http,
            IDataProtectionProvider dataProtection,
            IConfiguration config,
            IHostEnvironment env)
        {
            _http = http;
            _protector = dataProtection.CreateProtector("MyShop.SupportChatAccess.v1");
            _env = env;
            var master = config["Security:MasterKeys:v1"];
            _hmacKey = string.IsNullOrWhiteSpace(master)
                ? null
                : SHA256.HashData(Encoding.UTF8.GetBytes(master));
        }

        public void Issue(Guid chatId, int siteId)
        {
            var ctx = _http.HttpContext;
            if (ctx == null || chatId == Guid.Empty) return;

            var entries = ReadEntries(ctx);
            entries.RemoveAll(e => e.ChatId == chatId || e.ExpiresUtc < DateTime.UtcNow);
            entries.Add(new Entry
            {
                ChatId = chatId,
                SiteId = siteId,
                ExpiresUtc = DateTime.UtcNow.Add(Lifetime)
            });
            if (entries.Count > 8)
                entries = entries.OrderByDescending(e => e.ExpiresUtc).Take(8).ToList();

            var payload = string.Join(";", entries.Select(e =>
                $"{e.ChatId:N}|{e.SiteId}|{e.ExpiresUtc.Ticks}"));
            var sealedPayload = Seal(payload);
            _lastSealed = sealedPayload;

            try
            {
                ctx.Response.Cookies.Append(CookieName, sealedPayload, new CookieOptions
                {
                    HttpOnly = true,
                    IsEssential = true,
                    SameSite = SameSiteMode.Lax,
                    Secure = _env.IsDevelopment() ? ctx.Request.IsHttps : true,
                    Path = "/",
                    Expires = DateTimeOffset.UtcNow.Add(Lifetime)
                });
            }
            catch
            {
                /* مدار Blazor ممکن است پاسخ را قبلاً شروع کرده باشد؛ هدر اتصال Hub کافی است. */
            }
        }

        public bool TryAuthorize(Guid chatId, int? chatSiteId)
        {
            var ctx = _http.HttpContext;
            if (ctx == null || chatId == Guid.Empty) return false;
            var entries = ReadEntries(ctx);
            return entries.Any(e =>
                e.ChatId == chatId
                && e.ExpiresUtc >= DateTime.UtcNow
                && (!chatSiteId.HasValue || chatSiteId.Value == 0 || e.SiteId == chatSiteId.Value));
        }

        List<Entry> ReadEntries(HttpContext ctx)
        {
            if (!ctx.Request.Cookies.TryGetValue(CookieName, out var raw) || string.IsNullOrWhiteSpace(raw))
                return new List<Entry>();
            try
            {
                var payload = Unseal(raw);
                var list = new List<Entry>();
                foreach (var part in payload.Split(';', StringSplitOptions.RemoveEmptyEntries))
                {
                    var bits = part.Split('|');
                    if (bits.Length != 3) continue;
                    if (!Guid.TryParseExact(bits[0], "N", out var id)) continue;
                    if (!int.TryParse(bits[1], out var siteId)) continue;
                    if (!long.TryParse(bits[2], out var ticks)) continue;
                    list.Add(new Entry { ChatId = id, SiteId = siteId, ExpiresUtc = new DateTime(ticks, DateTimeKind.Utc) });
                }
                return list;
            }
            catch
            {
                return new List<Entry>();
            }
        }

        public void ConfigureHubConnection(Microsoft.AspNetCore.Http.Connections.Client.HttpConnectionOptions options)
        {
            var ctx = _http.HttpContext;
            var cookie = ctx?.Request.Headers.Cookie.ToString() ?? "";
            var access = _lastSealed
                ?? (ctx != null && ctx.Request.Cookies.TryGetValue(CookieName, out var existing) ? existing : null);
            if (!string.IsNullOrEmpty(access))
            {
                if (string.IsNullOrEmpty(cookie))
                    cookie = CookieName + "=" + access;
                else if (cookie.IndexOf(CookieName, StringComparison.OrdinalIgnoreCase) < 0)
                    cookie += "; " + CookieName + "=" + access;
                else
                {
                    // جایگزینی مقدار قبلی همین کوکی
                    cookie = System.Text.RegularExpressions.Regex.Replace(
                        cookie,
                        "(?i)(^|;\\s*)" + System.Text.RegularExpressions.Regex.Escape(CookieName) + "=([^;]*)",
                        "$1" + CookieName + "=" + access);
                }
            }
            if (!string.IsNullOrEmpty(cookie))
                options.Headers["Cookie"] = cookie;
        }

        string Seal(string payload)
        {
            if (_hmacKey != null)
            {
                var bytes = Encoding.UTF8.GetBytes(payload);
                using var hmac = new HMACSHA256(_hmacKey);
                var mac = hmac.ComputeHash(bytes);
                return "v1." + Convert.ToBase64String(bytes) + "." + Convert.ToBase64String(mac);
            }
            return "dp." + _protector.Protect(payload);
        }

        string Unseal(string sealedPayload)
        {
            if (sealedPayload.StartsWith("v1.", StringComparison.Ordinal))
            {
                var parts = sealedPayload.Split('.');
                if (parts.Length != 3 || _hmacKey == null) throw new InvalidOperationException();
                var bytes = Convert.FromBase64String(parts[1]);
                var mac = Convert.FromBase64String(parts[2]);
                using var hmac = new HMACSHA256(_hmacKey);
                var expected = hmac.ComputeHash(bytes);
                if (!CryptographicOperations.FixedTimeEquals(mac, expected))
                    throw new InvalidOperationException();
                return Encoding.UTF8.GetString(bytes);
            }
            if (sealedPayload.StartsWith("dp.", StringComparison.Ordinal))
                return _protector.Unprotect(sealedPayload.Substring(3));
            return _protector.Unprotect(sealedPayload);
        }

        sealed class Entry
        {
            public Guid ChatId { get; set; }
            public int SiteId { get; set; }
            public DateTime ExpiresUtc { get; set; }
        }
    }
}
