using MyShop.Application.Interfaces.Ai;
using MyShop.Core.Models.Ai;
using System;
using System.Threading.Tasks;

namespace MyShop.Application.Services.Ai
{
    /// <summary>
    /// Spend protection for a widget that is public on every page. Without a cap, one visitor can exhaust the
    /// whole API credit in a single night. Counted per identity per day, plus a site-wide token ceiling.
    /// </summary>
    public class AiUsageGuard : IAiUsageGuard
    {
        private readonly IAiUsageRepository _usage;

        public AiUsageGuard(IAiUsageRepository usage) => _usage = usage;

        public async Task<AiUsageDecision> CheckAsync(AiChatContext context, string message, AiRuntimeOptions options)
        {
            if (_usage == null || context == null) return AiUsageDecision.Allow;
            options ??= AiRuntimeOptions.Defaults;

            var siteId = context.SiteId ?? 0;
            var today = DateTime.Today;
            var (userKey, guestKey, ipKey) = Keys(context);

            AiUsageSnapshot snapshot;
            try
            {
                snapshot = await _usage.GetSnapshotAsync(siteId, today, userKey, guestKey, ipKey);
            }
            catch
            {
                // A quota table that is not migrated yet must not block support.
                return AiUsageDecision.Allow;
            }

            if (snapshot == null) return AiUsageDecision.Allow;

            if (options.MaxTokensPerSitePerDay > 0 && snapshot.SiteTokens >= options.MaxTokensPerSitePerDay)
            {
                return AiUsageDecision.Deny(
                    "سهمیه روزانه دستیار هوشمند این سایت تمام شده است. لطفاً با پشتیبانی انسانی گفتگو کنید یا فردا دوباره تلاش کنید.");
            }

            if (options.MaxMessagesPerIdentityPerDay > 0 && snapshot.IdentityMessages >= options.MaxMessagesPerIdentityPerDay)
            {
                return AiUsageDecision.Deny(
                    $"امروز به سقف {options.MaxMessagesPerIdentityPerDay} پیام دستیار هوشمند رسیده‌اید. برای ادامه با پشتیبانی انسانی گفتگو کنید.");
            }

            // The message itself is counted before the provider call, so a denied request costs nothing.
            await IncrementIdentityAsync(siteId, today, userKey, guestKey, ipKey);
            return AiUsageDecision.Allow;
        }

        public async Task RecordAsync(AiChatContext context, int promptTokens, int completionTokens)
        {
            if (_usage == null || context == null) return;
            if (promptTokens <= 0 && completionTokens <= 0) return;
            try
            {
                await _usage.IncrementAsync(context.SiteId ?? 0, DateTime.Today,
                    AiUsageScopes.Site, "site", 0, promptTokens, completionTokens);
            }
            catch { }
        }

        async Task IncrementIdentityAsync(int siteId, DateTime day, string userKey, string guestKey, string ipKey)
        {
            try
            {
                if (userKey != null)
                    await _usage.IncrementAsync(siteId, day, AiUsageScopes.User, userKey, 1, 0, 0);
                else if (guestKey != null)
                    await _usage.IncrementAsync(siteId, day, AiUsageScopes.Guest, guestKey, 1, 0, 0);
                else if (ipKey != null)
                    await _usage.IncrementAsync(siteId, day, AiUsageScopes.Ip, ipKey, 1, 0, 0);
            }
            catch { }
        }

        static (string UserKey, string GuestKey, string IpKey) Keys(AiChatContext context)
        {
            var userKey = context.UserId is > 0 ? context.UserId.Value.ToString() : null;
            var guestKey = Trim(context.GuestToken);
            var ipKey = Trim(context.ClientIp);
            return (userKey, guestKey, ipKey);
        }

        static string Trim(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            s = s.Trim();
            return s.Length <= 128 ? s : s.Substring(0, 128);
        }
    }
}
