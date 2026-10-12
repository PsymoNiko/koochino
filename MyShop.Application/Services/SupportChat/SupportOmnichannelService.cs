using MyShop.Application.Interfaces.SupportChat;
using MyShop.Core.Models.SupportChat;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Application.Services.SupportChat
{
    public class SupportOmnichannelService : ISupportOmnichannelService
    {
        private readonly ISupportChatService _chats;
        private readonly ISupportChannelOutbound _outbound;

        public SupportOmnichannelService(ISupportChatService chats, ISupportChannelOutbound outbound = null)
        {
            _chats = chats;
            _outbound = outbound;
        }

        public async Task<bool> IsEnabledAsync(int siteId)
        {
            if (siteId <= 0) return false;
            try
            {
                var s = await _chats.GetSettingsAsync(siteId);
                return s != null && s.OmnichannelEnabled;
            }
            catch { return false; }
        }

        public async Task<bool> ShouldInterceptFreeTextAsync(int siteId)
        {
            if (siteId <= 0) return false;
            try
            {
                var s = await _chats.GetSettingsAsync(siteId);
                return s != null && s.OmnichannelEnabled && s.OmnichannelInterceptFreeText;
            }
            catch { return false; }
        }

        public async Task HandleInboundAsync(string provider, int siteId, string externalChatId, string visitorName, string text)
        {
            if (string.IsNullOrWhiteSpace(externalChatId) || string.IsNullOrWhiteSpace(text)) return;
            if (siteId <= 0) return;
            if (!await IsEnabledAsync(siteId)) return;

            provider = NormalizeProvider(provider);
            var token = provider + ":" + externalChatId.Trim();
            var thread = await _chats.StartOrGetAsync(new SupportChatStartRequest
            {
                SiteId = siteId,
                GuestToken = token,
                VisitorName = string.IsNullOrWhiteSpace(visitorName) ? provider : visitorName.Trim(),
                Audience = "Guest",
                Channel = provider,
                ExternalThreadId = externalChatId.Trim(),
                CurrentPageUrl = provider
            });
            if (thread == null) return;

            try { await _chats.UpdateCurrentPageAsync(thread.Id, provider + ":" + externalChatId); } catch { }

            var userMsg = await _chats.SendFromUserAsync(thread.Id, null, text.Trim());
            if (userMsg == null) return;

            if (_outbound == null) return;
            var all = await _chats.GetMessagesAsync(thread.Id, userMsg.Id);
            var replies = all.Where(m =>
                string.Equals(m.SenderType, SupportChatSenders.Ai, StringComparison.OrdinalIgnoreCase)
                || string.Equals(m.SenderType, SupportChatSenders.System, StringComparison.OrdinalIgnoreCase)).ToList();
            if (replies.Count == 0)
                await _outbound.SendToExternalAsync(provider, externalChatId, "پیام شما به پشتیبانی رسید. به‌زودی پاسخ می‌دهیم.");
            else
            {
                foreach (var r in replies)
                    await _outbound.SendToExternalAsync(provider, externalChatId, r.Body);
            }
        }

        public static string NormalizeProvider(string provider)
        {
            if (string.Equals(provider, "Telegram", StringComparison.OrdinalIgnoreCase)) return SupportChannels.Telegram;
            if (string.Equals(provider, "Bale", StringComparison.OrdinalIgnoreCase)) return SupportChannels.Bale;
            return (provider ?? SupportChannels.Website).Trim();
        }

        public static bool TryParseExternal(SupportChatThread chat, out string provider, out string chatId)
        {
            provider = chat?.Channel;
            chatId = null;
            var token = chat?.GuestToken;
            if (string.IsNullOrWhiteSpace(token) || !token.Contains(':')) return false;
            var i = token.IndexOf(':');
            provider = token.Substring(0, i);
            chatId = token.Substring(i + 1);
            return !string.IsNullOrWhiteSpace(chatId)
                && (string.Equals(provider, SupportChannels.Bale, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(provider, SupportChannels.Telegram, StringComparison.OrdinalIgnoreCase));
        }
    }
}
