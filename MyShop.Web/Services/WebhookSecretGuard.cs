using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using MyShop.Application.Interfaces.Security;
using MyShop.Core.Models.Security;

namespace MyShop.Web.Services
{
    /// <summary>5a Monitor / 5c Enforce (فلگ DB، پیش‌فرض خاموش).</summary>
    public static class WebhookSecretGuard
    {
        public static string ReadProvided(HttpContext ctx) =>
            ctx.Request.Headers["X-Telegram-Bot-Api-Secret-Token"].FirstOrDefault()
            ?? ctx.Request.Headers["X-Bale-Secret"].FirstOrDefault()
            ?? ctx.Request.Query["secret"].FirstOrDefault()
            ?? "";

        public static async Task<IResult> CheckAsync(
            HttpContext ctx,
            string expectedSecret,
            int? siteId,
            string moduleKey,
            string settingKey)
        {
            var audit = ctx.RequestServices.GetService<IModuleSettingRepository>();
            var enforce = false;
            if (siteId.HasValue && siteId.Value > 0 && audit != null)
            {
                try { enforce = await audit.GetWebhookEnforceAsync(siteId.Value); }
                catch { enforce = false; }
            }

            var provided = ReadProvided(ctx);
            var expected = (expectedSecret ?? "").Trim();

            if (string.IsNullOrEmpty(expected))
            {
                var stored = false;
                if (audit != null)
                {
                    try
                    {
                        stored = await audit.HasSecretStoredAsync(siteId, moduleKey, settingKey);
                        if (!stored)
                            stored = await audit.HasSecretStoredAsync(null, moduleKey, settingKey);
                    }
                    catch { stored = false; }
                }

                var failClosed = enforce || stored;
                await WriteAsync(audit, ctx, failClosed ? "WebhookDeny" : "WebhookWarn", moduleKey, settingKey, siteId, success: !failClosed);
                if (failClosed)
                    return Results.Unauthorized();
                return null;
            }

            if (!FixedEquals(provided, expected))
            {
                await WriteAsync(audit, ctx, "WebhookDeny", moduleKey, settingKey, siteId, success: false);
                return Results.Unauthorized();
            }

            return null;
        }

        static bool FixedEquals(string a, string b)
        {
            a ??= "";
            b ??= "";
            var ba = Encoding.UTF8.GetBytes(a);
            var bb = Encoding.UTF8.GetBytes(b);
            if (ba.Length != bb.Length)
            {
                CryptographicOperations.FixedTimeEquals(ba, ba);
                return false;
            }
            return CryptographicOperations.FixedTimeEquals(ba, bb);
        }

        static Task WriteAsync(IModuleSettingRepository repo, HttpContext ctx, string action, string module, string key, int? siteId, bool success)
        {
            if (repo == null) return Task.CompletedTask;
            return repo.WriteAuditAsync(new SecretAuditEntry
            {
                Action = action,
                ModuleKey = module,
                SettingKey = key,
                SiteId = siteId,
                ActorUserId = null,
                ActorRole = "Webhook",
                ClientIp = ctx.Connection?.RemoteIpAddress?.ToString(),
                UserAgent = Trunc(ctx.Request.Headers.UserAgent.ToString(), 500),
                Success = success
            });
        }

        static string Trunc(string s, int n)
        {
            if (string.IsNullOrEmpty(s) || s.Length <= n) return s;
            return s.Substring(0, n);
        }
    }
}
