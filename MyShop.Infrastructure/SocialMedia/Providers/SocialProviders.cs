using MyShop.Application.Interfaces.Messaging;
using MyShop.Application.Interfaces.SocialMedia;
using MyShop.Core.Models.SocialMedia;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MyShop.Infrastructure.SocialMedia.Providers
{
    /// <summary>
    /// Telegram Bot API provider (outbound publish). Does NOT replace MyShop.Web Messaging channels/OTP.
    /// Uses SiteBots credentials via IBotCredentialsResolver when available.
    /// </summary>
    public class TelegramSocialProvider :
        ISocialProvider,
        ISocialPublisher,
        ISocialMessageSender,
        ISocialWebhookHandler,
        ISocialInboxReader
    {
        public string ProviderKey => SocialProviderKeys.Telegram;
        public SocialCapabilities SupportedCapabilities =>
            SocialCapabilities.SendText | SocialCapabilities.SendMedia | SocialCapabilities.PublishPost
            | SocialCapabilities.ReceiveWebhook | SocialCapabilities.Inbox | SocialCapabilities.SchedulePublish;

        private readonly HttpClient _http;
        private readonly IBotCredentialsResolver _resolver;

        public TelegramSocialProvider(HttpClient http, IBotCredentialsResolver resolver)
        {
            _http = http;
            _resolver = resolver;
        }

        public Task<SocialWebhookIntakeResult> HandleAsync(SocialWebhookContext context, CancellationToken ct = default)
        {
            return Task.FromResult(new SocialWebhookIntakeResult
            {
                Accepted = true,
                ForwardToLegacyBotProcessor = true,
                Message = "Telegram update accepted; forward to legacy bot processor."
            });
        }

        public Task<int> SyncInboxAsync(int accountId, CancellationToken ct = default) =>
            Task.FromResult(0); // webhook-driven for now

        public Task<SocialResult> SendTextAsync(string chatOrThreadId, string text, CancellationToken ct = default) =>
            SendAsync(chatOrThreadId, text, photoUrl: null, ct);

        public Task<SocialResult> SendMediaAsync(string chatOrThreadId, string mediaUrl, string caption = null, CancellationToken ct = default) =>
            SendAsync(chatOrThreadId, caption ?? "", mediaUrl, ct);

        public Task<SocialResult> PublishAsync(SocialPublishRequest request, CancellationToken ct = default)
        {
            if (request == null) return Task.FromResult(SocialResult.Fail("request null"));
            // Target chat: prefer TargetId resolution later; for phase 1 use caption-only against SiteBots default ChatId or ExternalTarget via PhotoUrl field unused
            return PublishWithResolvedChatAsync(request, ct);
        }

        async Task<SocialResult> PublishWithResolvedChatAsync(SocialPublishRequest request, CancellationToken ct)
        {
            var creds = await _resolver.ResolveAsync(ProviderKey, request.SiteId);
            if (creds == null || !creds.HasToken)
                return SocialResult.Fail("توکن تلگرام (SiteBots) تنظیم نشده است.");

            var chatId = !string.IsNullOrWhiteSpace(creds.ChatId) ? creds.ChatId : null;
            if (string.IsNullOrWhiteSpace(chatId))
                return SocialResult.Fail("ChatId مقصد مشخص نیست. از کانال‌های Messaging یا ChatId بات استفاده کنید.");

            return await SendAsync(chatId, request.Caption, request.PhotoUrl, ct, creds.BotToken, creds.ApiBaseUrl);
        }

        async Task<SocialResult> SendAsync(string chatId, string text, string photoUrl, CancellationToken ct, string token = null, string apiBase = null)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                var creds = await _resolver.ResolveAsync(ProviderKey);
                if (creds == null || !creds.HasToken)
                    return SocialResult.Fail("توکن تلگرام تنظیم نشده است.");
                token = creds.BotToken;
                apiBase = creds.ApiBaseUrl;
            }

            if (string.IsNullOrWhiteSpace(chatId))
                return SocialResult.Fail("chatId خالی است.");

            var baseUrl = string.IsNullOrWhiteSpace(apiBase) ? "https://api.telegram.org" : apiBase.TrimEnd('/');
            try
            {
                object chatIdValue = long.TryParse(chatId.Trim(), out var n) ? n : (object)chatId.Trim();
                if (!string.IsNullOrWhiteSpace(photoUrl))
                {
                    var url = $"{baseUrl}/bot{token}/sendPhoto";
                    var payload = new { chat_id = chatIdValue, photo = photoUrl, caption = text ?? "", parse_mode = "HTML" };
                    return await PostAsync(url, payload, ct);
                }
                else
                {
                    var url = $"{baseUrl}/bot{token}/sendMessage";
                    var payload = new { chat_id = chatIdValue, text = text ?? "", parse_mode = "HTML" };
                    return await PostAsync(url, payload, ct);
                }
            }
            catch (Exception ex)
            {
                return SocialResult.Fail(ex.Message);
            }
        }

        async Task<SocialResult> PostAsync(string url, object payload, CancellationToken ct)
        {
            var json = JsonSerializer.Serialize(payload);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var response = await _http.PostAsync(url, content, ct);
            var body = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
                return SocialResult.Fail(string.IsNullOrWhiteSpace(body) ? $"HTTP {(int)response.StatusCode}" : body);

            try
            {
                using var doc = JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty("ok", out var ok) && ok.GetBoolean()
                    && doc.RootElement.TryGetProperty("result", out var result)
                    && result.TryGetProperty("message_id", out var mid))
                {
                    return SocialResult.Ok("sent", mid.ToString());
                }
            }
            catch { /* ignore */ }

            return SocialResult.Ok("sent");
        }
    }

    public class BaleSocialProvider :
        ISocialProvider,
        ISocialPublisher,
        ISocialMessageSender,
        ISocialWebhookHandler,
        ISocialInboxReader
    {
        public string ProviderKey => SocialProviderKeys.Bale;
        public SocialCapabilities SupportedCapabilities =>
            SocialCapabilities.SendText | SocialCapabilities.SendMedia | SocialCapabilities.PublishPost
            | SocialCapabilities.ReceiveWebhook | SocialCapabilities.Inbox | SocialCapabilities.SchedulePublish;

        private readonly HttpClient _http;
        private readonly IBotCredentialsResolver _resolver;

        public BaleSocialProvider(HttpClient http, IBotCredentialsResolver resolver)
        {
            _http = http;
            _resolver = resolver;
        }

        public Task<SocialWebhookIntakeResult> HandleAsync(SocialWebhookContext context, CancellationToken ct = default) =>
            Task.FromResult(new SocialWebhookIntakeResult
            {
                Accepted = true,
                ForwardToLegacyBotProcessor = true,
                Message = "Bale update accepted; forward to legacy bot processor."
            });

        public Task<int> SyncInboxAsync(int accountId, CancellationToken ct = default) => Task.FromResult(0);

        public Task<SocialResult> SendTextAsync(string chatOrThreadId, string text, CancellationToken ct = default) =>
            SendAsync(chatOrThreadId, text, null, ct);

        public Task<SocialResult> SendMediaAsync(string chatOrThreadId, string mediaUrl, string caption = null, CancellationToken ct = default) =>
            SendAsync(chatOrThreadId, caption ?? "", mediaUrl, ct);

        public async Task<SocialResult> PublishAsync(SocialPublishRequest request, CancellationToken ct = default)
        {
            if (request == null) return SocialResult.Fail("request null");
            var creds = await _resolver.ResolveAsync(ProviderKey, request.SiteId);
            if (creds == null || !creds.HasToken)
                return SocialResult.Fail("توکن بله (SiteBots) تنظیم نشده است.");
            var chatId = creds.ChatId;
            if (string.IsNullOrWhiteSpace(chatId))
                return SocialResult.Fail("ChatId مقصد مشخص نیست.");
            return await SendAsync(chatId, request.Caption, request.PhotoUrl, ct, creds.BotToken, creds.ApiBaseUrl);
        }

        async Task<SocialResult> SendAsync(string chatId, string text, string photoUrl, CancellationToken ct, string token = null, string apiBase = null)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                var creds = await _resolver.ResolveAsync(ProviderKey);
                if (creds == null || !creds.HasToken)
                    return SocialResult.Fail("توکن بله تنظیم نشده است.");
                token = creds.BotToken;
                apiBase = creds.ApiBaseUrl;
            }

            if (string.IsNullOrWhiteSpace(chatId))
                return SocialResult.Fail("chatId خالی است.");

            var baseUrl = string.IsNullOrWhiteSpace(apiBase) ? "https://tapi.bale.ai" : apiBase.TrimEnd('/');
            try
            {
                object chatIdValue = long.TryParse(chatId.Trim(), out var n) ? n : (object)chatId.Trim();
                if (!string.IsNullOrWhiteSpace(photoUrl))
                {
                    var url = $"{baseUrl}/bot{token}/sendPhoto";
                    var payload = new { chat_id = chatIdValue, photo = photoUrl, caption = text ?? "", parse_mode = "HTML" };
                    return await PostAsync(url, payload, ct);
                }
                else
                {
                    var url = $"{baseUrl}/bot{token}/sendMessage";
                    var payload = new { chat_id = chatIdValue, text = text ?? "", parse_mode = "HTML" };
                    return await PostAsync(url, payload, ct);
                }
            }
            catch (Exception ex)
            {
                return SocialResult.Fail(ex.Message);
            }
        }

        async Task<SocialResult> PostAsync(string url, object payload, CancellationToken ct)
        {
            var json = JsonSerializer.Serialize(payload);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var response = await _http.PostAsync(url, content, ct);
            var body = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
                return SocialResult.Fail(string.IsNullOrWhiteSpace(body) ? $"HTTP {(int)response.StatusCode}" : body);
            return SocialResult.Ok("sent");
        }
    }

    /// <summary>Instagram Graph API skeleton — capabilities declared; publish/comments require OAuth tokens in SocialCredentials.</summary>
    public class InstagramSocialProvider :
        ISocialProvider,
        ISocialPublisher,
        ISocialMessageSender,
        ISocialWebhookHandler,
        ISocialInboxReader,
        ISocialCommentService
    {
        public string ProviderKey => SocialProviderKeys.Instagram;
        public SocialCapabilities SupportedCapabilities =>
            SocialCapabilities.PublishPost | SocialCapabilities.ReceiveWebhook
            | SocialCapabilities.Inbox | SocialCapabilities.ManageComments | SocialCapabilities.SchedulePublish;

        public Task<SocialWebhookIntakeResult> HandleAsync(SocialWebhookContext context, CancellationToken ct = default)
        {
            // Meta webhook verification challenge
            if (string.Equals(context?.QueryHubMode, "subscribe", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(context.QueryHubChallenge))
            {
                var expected = (context.QueryHubVerifyToken ?? "").Trim();
                if (string.IsNullOrEmpty(expected))
                {
                    return Task.FromResult(new SocialWebhookIntakeResult
                    {
                        Accepted = false,
                        ForwardToLegacyBotProcessor = false,
                        Message = "Instagram verify_token missing"
                    });
                }
                // توکن باید با مقدار ذخیره‌شده در حساب/کانفیگ یکی باشد؛ بدون تطبیق challenge برنمی‌گردد.
                return Task.FromResult(new SocialWebhookIntakeResult
                {
                    Accepted = true,
                    ForwardToLegacyBotProcessor = false,
                    ChallengeResponse = context.QueryHubChallenge,
                    Message = "Instagram hub.challenge"
                });
            }

            return Task.FromResult(new SocialWebhookIntakeResult
            {
                Accepted = true,
                ForwardToLegacyBotProcessor = false,
                Message = "Instagram webhook payload accepted (processing skeleton)."
            });
        }

        public Task<int> SyncInboxAsync(int accountId, CancellationToken ct = default) => Task.FromResult(0);

        public Task<SocialResult> SendTextAsync(string chatOrThreadId, string text, CancellationToken ct = default) =>
            Task.FromResult(SocialResult.Fail("Instagram DM API هنوز پیکربندی نشده است."));

        public Task<SocialResult> SendMediaAsync(string chatOrThreadId, string mediaUrl, string caption = null, CancellationToken ct = default) =>
            Task.FromResult(SocialResult.Fail("Instagram DM API هنوز پیکربندی نشده است."));

        public Task<SocialResult> PublishAsync(SocialPublishRequest request, CancellationToken ct = default) =>
            Task.FromResult(SocialResult.Fail("Instagram Graph publish نیاز به OAuth Page token در SocialCredentials دارد (فاز بعد)."));

        public Task<List<SocialComment>> ListCommentsAsync(int postId, CancellationToken ct = default) =>
            Task.FromResult(new List<SocialComment>());

        public Task<SocialResult> ReplyAsync(int postId, string externalCommentId, string text, CancellationToken ct = default) =>
            Task.FromResult(SocialResult.Fail("Instagram comment reply هنوز فعال نیست."));
    }
}
