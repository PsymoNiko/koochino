using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using MyShop.Application.Interfaces.Messaging;
using MyShop.Application.Interfaces.Security;
using MyShop.Application.Services.Security;
using MyShop.Core.Interfaces;
using MyShop.Core.Models.Messaging;
using System;
using System.Threading.Tasks;

namespace MyShop.Web.Services
{
    public class BotCredentialsResolver : IBotCredentialsResolver
    {
        static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

        private readonly ISiteBotRepository _repo;
        private readonly IConfiguration _config;
        private readonly IMemoryCache _cache;
        private readonly ICurrentTenant _tenant;
        private readonly IModuleConfigService _moduleConfig;

        public BotCredentialsResolver(
            ISiteBotRepository repo,
            IConfiguration config,
            IMemoryCache cache,
            ICurrentTenant tenant = null,
            IModuleConfigService moduleConfig = null)
        {
            _repo = repo;
            _config = config;
            _cache = cache;
            _tenant = tenant;
            _moduleConfig = moduleConfig;
        }

        public async Task<BotCredentials> ResolveAsync(string provider, int? siteId = null)
        {
            provider = string.IsNullOrWhiteSpace(provider) ? MessagingProviders.Bale : provider.Trim();
            var sid = siteId ?? (_tenant != null && _tenant.HasSite ? _tenant.SiteId : 0);

            if (BotExecutionContext.Current != null
                && string.Equals(BotExecutionContext.Current.Provider, provider, StringComparison.OrdinalIgnoreCase)
                && (sid <= 0 || BotExecutionContext.Current.SiteId == sid))
            {
                return BotExecutionContext.Current;
            }

            if (sid > 0)
            {
                var key = $"botcreds:{sid}:{provider}";
                if (_cache.TryGetValue(key, out BotCredentials cached) && cached != null)
                    return cached;

                try
                {
                    var row = await _repo.GetAsync(sid, provider);
                    if (row != null && row.IsActive)
                    {
                        var mapped = Map(row);
                        await OverlayModuleSecretsAsync(mapped, sid, provider);
                        MergeConfigFallback(mapped, provider);
                        _cache.Set(key, mapped, CacheTtl);
                        return mapped;
                    }
                }
                catch { /* table missing */ }
            }

            var creds = ResolveFromConfigFallback(provider);
            await OverlayModuleSecretsAsync(creds, sid > 0 ? sid : ModuleConfigRead.SiteIdOf(_tenant) ?? 0, provider);
            return creds;
        }

        async Task OverlayModuleSecretsAsync(BotCredentials creds, int siteId, string provider)
        {
            if (creds == null || _moduleConfig == null) return;
            var prefix = provider.Equals(MessagingProviders.Telegram, StringComparison.OrdinalIgnoreCase) ? "Telegram"
                : provider.Equals(MessagingProviders.Eita, StringComparison.OrdinalIgnoreCase) ? "Eita"
                : "Bale";
            var sid = siteId > 0 ? siteId : (int?)null;
            var token = await ModuleConfigRead.TryGetAsync(_moduleConfig, sid, "Messaging", prefix + ".BotToken", true, allowLegacyFallback: false);
            if (!string.IsNullOrWhiteSpace(token))
                creds.BotToken = token;
            var hook = await ModuleConfigRead.TryGetAsync(_moduleConfig, sid, "Messaging", prefix + ".WebhookSecret", true, allowLegacyFallback: false);
            if (!string.IsNullOrWhiteSpace(hook))
                creds.WebhookSecret = hook;
        }

        public BotCredentials ResolveFromConfigFallback(string provider)
        {
            provider = string.IsNullOrWhiteSpace(provider) ? MessagingProviders.Bale : provider.Trim();
            var section = provider.Equals(MessagingProviders.Telegram, StringComparison.OrdinalIgnoreCase)
                ? "Telegram"
                : provider.Equals(MessagingProviders.Eita, StringComparison.OrdinalIgnoreCase)
                    ? "Eita"
                    : "Bale";

            var defaultBase = section == "Telegram" ? "https://api.telegram.org"
                : section == "Eita" ? "https://eitaayar.ir/api"
                : "https://tapi.bale.ai";

            return new BotCredentials
            {
                SiteId = _tenant?.SiteId ?? 0,
                SiteCode = _tenant?.SiteCode ?? "",
                Provider = provider,
                BotToken = (_config[$"{section}:BotToken"] ?? "").Trim(),
                ApiBaseUrl = (_config[$"{section}:ApiBaseUrl"] ?? defaultBase).Trim().TrimEnd('/'),
                ChatId = _config[$"{section}:ChatId"],
                BotLink = _config[$"{section}:BotLink"],
                WebhookSecret = _config[$"{section}:WebhookSecret"],
                DisplayName = section,
                IsActive = true
            };
        }

        static BotCredentials Map(SiteBot row)
        {
            var provider = row.Provider ?? "";
            var defaultBase = provider.Equals(MessagingProviders.Telegram, StringComparison.OrdinalIgnoreCase)
                ? "https://api.telegram.org"
                : provider.Equals(MessagingProviders.Eita, StringComparison.OrdinalIgnoreCase)
                    ? "https://eitaayar.ir/api"
                    : "https://tapi.bale.ai";

            return new BotCredentials
            {
                SiteId = row.SiteId,
                SiteCode = row.SiteCode ?? "",
                Provider = row.Provider,
                BotToken = (row.BotToken ?? "").Trim(),
                ApiBaseUrl = string.IsNullOrWhiteSpace(row.ApiBaseUrl)
                    ? defaultBase
                    : row.ApiBaseUrl.Trim().TrimEnd('/'),
                ChatId = row.ChatId,
                BotLink = row.BotLink,
                WebhookSecret = row.WebhookSecret,
                DisplayName = row.DisplayName,
                IsActive = row.IsActive
            };
        }

        void MergeConfigFallback(BotCredentials creds, string provider)
        {
            var fb = ResolveFromConfigFallback(provider);
            if (!creds.HasToken && fb.HasToken) creds.BotToken = fb.BotToken;
            if (string.IsNullOrWhiteSpace(creds.ChatId)) creds.ChatId = fb.ChatId;
            if (string.IsNullOrWhiteSpace(creds.BotLink)) creds.BotLink = fb.BotLink;
            if (string.IsNullOrWhiteSpace(creds.WebhookSecret)) creds.WebhookSecret = fb.WebhookSecret;

            // اگر ApiBaseUrl مستقیم API فیلترشده است، Worker از appsettings را ترجیح بده
            var needsProxy = string.IsNullOrWhiteSpace(creds.ApiBaseUrl)
                || creds.ApiBaseUrl.Contains("tapi.bale.ai", StringComparison.OrdinalIgnoreCase)
                || creds.ApiBaseUrl.Contains("api.telegram.org", StringComparison.OrdinalIgnoreCase);
            if (needsProxy && !string.IsNullOrWhiteSpace(fb.ApiBaseUrl)
                && fb.ApiBaseUrl.IndexOf("workers.dev", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                creds.ApiBaseUrl = fb.ApiBaseUrl;
            }
            else if (string.IsNullOrWhiteSpace(creds.ApiBaseUrl) && !string.IsNullOrWhiteSpace(fb.ApiBaseUrl))
            {
                creds.ApiBaseUrl = fb.ApiBaseUrl;
            }
        }
    }
}
