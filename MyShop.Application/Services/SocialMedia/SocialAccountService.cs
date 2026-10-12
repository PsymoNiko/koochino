using MyShop.Application.Interfaces.Messaging;
using MyShop.Application.Interfaces.SocialMedia;
using MyShop.Core.Models.Messaging;
using MyShop.Core.Models.SocialMedia;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Application.Services.SocialMedia
{
    public class SocialAccountService : ISocialAccountService
    {
        private readonly ISocialAccountRepository _accounts;
        private readonly ISiteBotRepository _siteBots;
        private readonly ISocialProviderResolver _providers;
        private readonly ISocialOperationLogService _logs;

        public SocialAccountService(
            ISocialAccountRepository accounts,
            ISiteBotRepository siteBots,
            ISocialProviderResolver providers,
            ISocialOperationLogService logs)
        {
            _accounts = accounts;
            _siteBots = siteBots;
            _providers = providers;
            _logs = logs;
        }

        public async Task<List<SocialAccount>> ListConnectedAsync(int? siteId = null, string provider = null, bool includeSiteBotBridge = true)
        {
            var list = await _accounts.ListAsync(siteId, provider, activeOnly: false) ?? new List<SocialAccount>();
            if (!includeSiteBotBridge)
                return list;

            List<SiteBot> bots;
            try
            {
                bots = await _siteBots.ListAsync(siteId, provider, activeOnly: false) ?? new List<SiteBot>();
            }
            catch
            {
                bots = new List<SiteBot>();
            }

            var linkedBotIds = new HashSet<int>(list.Where(a => a.LinkedSiteBotId.HasValue).Select(a => a.LinkedSiteBotId.Value));
            foreach (var bot in bots)
            {
                if (linkedBotIds.Contains(bot.Id))
                    continue;
                // Avoid duplicate display when ExternalId already matches SiteBot id string
                if (list.Any(a => a.SiteId == bot.SiteId
                    && string.Equals(a.Provider, bot.Provider, StringComparison.OrdinalIgnoreCase)
                    && a.LinkedSiteBotId == bot.Id))
                    continue;

                list.Add(new SocialAccount
                {
                    Id = 0,
                    SiteId = bot.SiteId,
                    Provider = bot.Provider,
                    AccountType = SocialAccountTypes.Bot,
                    ExternalId = "sitebot:" + bot.Id,
                    DisplayName = string.IsNullOrWhiteSpace(bot.DisplayName) ? $"{bot.Provider} bot" : bot.DisplayName,
                    Username = null,
                    IsActive = bot.IsActive,
                    LinkedSiteBotId = bot.Id,
                    Notes = "از SiteBots (Messaging) — مدیریت از /admin/site-bots",
                    CreatedAt = bot.CreatedAt,
                    UpdatedAt = bot.UpdatedAt,
                    SiteCode = bot.SiteCode,
                    SiteName = bot.SiteName,
                    IsLinkedFromSiteBot = true
                });
            }

            return list
                .OrderBy(a => a.SiteId)
                .ThenBy(a => a.Provider)
                .ThenBy(a => a.DisplayName)
                .ToList();
        }

        public Task<SocialAccount> GetAsync(int id) => _accounts.GetByIdAsync(id);

        public async Task<int> SaveAsync(SocialAccount account)
        {
            if (account == null) throw new ArgumentNullException(nameof(account));
            if (account.IsLinkedFromSiteBot || (account.Id == 0 && account.ExternalId != null && account.ExternalId.StartsWith("sitebot:", StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("حساب‌های پل SiteBots را از صفحه بات‌های سایت ویرایش کنید.");

            var id = await _accounts.UpsertAsync(account);
            await _logs.LogAsync(account.SiteId, account.Provider, id, "AccountSave", true, account.DisplayName);
            return id;
        }

        public async Task SetActiveAsync(int id, bool isActive)
        {
            await _accounts.SetActiveAsync(id, isActive);
            var acc = await _accounts.GetByIdAsync(id);
            await _logs.LogAsync(acc?.SiteId, acc?.Provider, id, "AccountSetActive", true, isActive.ToString());
        }

        public async Task DeleteAsync(int id)
        {
            var acc = await _accounts.GetByIdAsync(id);
            await _accounts.DeleteAsync(id);
            await _logs.LogAsync(acc?.SiteId, acc?.Provider, id, "AccountDelete", true, null);
        }

        public Task<IReadOnlyList<ISocialProvider>> ListProvidersAsync() =>
            Task.FromResult(_providers.ListProviders());
    }
}
