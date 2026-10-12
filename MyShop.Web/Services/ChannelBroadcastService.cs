using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MyShop.Application.Interfaces.Messaging;
using MyShop.Core.Models.Messaging;

namespace MyShop.Web.Services
{
    /// <summary>ارسال پیام به کانال‌های بله / تلگرام / ایتا.</summary>
    public class ChannelBroadcastService : IChannelBroadcastService
    {
        private readonly IMessagingOutboundChannelRepository _repo;
        private readonly IBotCredentialsResolver _bots;
        private readonly IMessagingSendLogRepository _logs;
        private readonly HttpClient _http;
        private readonly ILogger<ChannelBroadcastService> _logger;

        public ChannelBroadcastService(
            IMessagingOutboundChannelRepository repo,
            IBotCredentialsResolver bots,
            IMessagingSendLogRepository logs,
            HttpClient http,
            ILogger<ChannelBroadcastService> logger = null)
        {
            _repo = repo;
            _bots = bots;
            _logs = logs;
            _http = http;
            _logger = logger;
        }

        public Task<List<MessagingOutboundChannel>> ListChannelsAsync(string provider = null, int? siteId = null)
            => _repo.ListAsync(provider, siteId, activeOnly: false);

        public Task<int> SaveChannelAsync(MessagingOutboundChannel channel) => _repo.UpsertAsync(channel);

        public Task DeleteChannelAsync(int id) => _repo.DeleteAsync(id);

        public async Task<(bool Ok, string ChatId, string Message)> ResolveChatIdAsync(int channelId, int? siteId = null)
        {
            var ch = await _repo.GetByIdAsync(channelId);
            if (ch == null) return (false, null, "کانال یافت نشد.");

            if (IsEita(ch.Provider))
                return (false, ch.EffectiveChatId, "کشف ChatId برای ایتا پشتیبانی نمی‌شود؛ شناسه عددی یا یوزرنیم کانال را دستی وارد کنید.");

            var creds = await _bots.ResolveAsync(ch.Provider, siteId ?? ch.SiteId);
            if (creds == null || !creds.HasToken)
                return (false, null, "توکن بات برای این پیام‌رسان تنظیم نشده است.");

            var target = NormalizeChatId(ch.Provider, ch.EffectiveChatId);
            if (string.IsNullOrWhiteSpace(target))
                return (false, null, "یوزرنیم کانال خالی است.");

            var (ok, resolved, err) = await GetChatAsync(creds, target);
            if (!ok)
                return (false, target, "getChat ناموفق: " + (err ?? "خطا") + " — می‌توانید مستقیم با " + target + " ارسال کنید.");

            await _repo.UpdateChatIdAsync(channelId, resolved);
            return (true, resolved, "ChatId ذخیره شد: " + resolved);
        }

        public async Task<(bool Ok, string Message)> SendAsync(int channelId, string text, int? siteId = null, string photoUrl = null)
        {
            if (string.IsNullOrWhiteSpace(text) && string.IsNullOrWhiteSpace(photoUrl))
                return (false, "متن یا تصویر پیام خالی است.");

            var ch = await _repo.GetByIdAsync(channelId);
            if (ch == null || !ch.IsActive)
                return (false, "کانال یافت نشد یا غیرفعال است.");

            return await SendQuickAsync(ch.Provider, ch.EffectiveChatId, text, siteId ?? ch.SiteId, photoUrl);
        }

        public async Task<(bool Ok, string Message)> SendQuickAsync(string provider, string usernameOrChatId, string text, int? siteId = null, string photoUrl = null)
        {
            if (string.IsNullOrWhiteSpace(text) && string.IsNullOrWhiteSpace(photoUrl))
                return (false, "متن یا تصویر پیام خالی است.");
            if (string.IsNullOrWhiteSpace(usernameOrChatId))
                return (false, "آدرس کانال خالی است.");

            provider = string.IsNullOrWhiteSpace(provider) ? MessagingProviders.Telegram : provider.Trim();
            var creds = await _bots.ResolveAsync(provider, siteId);
            if (creds == null || !creds.HasToken)
                return (false, $"توکن بات {provider} تنظیم نشده (SiteBots یا appsettings).");

            var chatId = NormalizeChatId(provider, usernameOrChatId);
            var caption = (text ?? "").Trim();
            var (ok, err) = string.IsNullOrWhiteSpace(photoUrl)
                ? await SendMessageAsync(creds, chatId, caption)
                : await SendPhotoAsync(creds, chatId, photoUrl.Trim(), caption);

            try
            {
                var logText = string.IsNullOrWhiteSpace(photoUrl)
                    ? caption
                    : $"[photo] {caption}";
                await _logs.InsertAsync(new MessagingSendLog
                {
                    Provider = provider,
                    PhoneNumber = null,
                    ChatId = chatId,
                    MessageType = "Channel",
                    MessageText = logText.Length > 500 ? logText.Substring(0, 500) : logText,
                    Success = ok,
                    ErrorMessage = err,
                    CreatedAt = DateTime.UtcNow
                });
            }
            catch { /* log optional */ }

            return ok
                ? (true, $"پیام با موفقیت به {chatId} از طریق {provider} ارسال شد.")
                : (false, err ?? "ارسال ناموفق");
        }

        public async Task<List<ChannelBroadcastResultItem>> SendToAllActiveAsync(string text, int? siteId = null, string photoUrl = null)
        {
            var results = new List<ChannelBroadcastResultItem>();
            if (string.IsNullOrWhiteSpace(text) && string.IsNullOrWhiteSpace(photoUrl))
            {
                results.Add(new ChannelBroadcastResultItem
                {
                    Ok = false,
                    Message = "متن یا تصویر پیام خالی است."
                });
                return results;
            }

            var channels = await _repo.ListAsync(null, siteId, activeOnly: true) ?? new List<MessagingOutboundChannel>();
            if (channels.Count == 0)
            {
                results.Add(new ChannelBroadcastResultItem
                {
                    Ok = false,
                    Message = "کانال فعالی یافت نشد."
                });
                return results;
            }

            foreach (var ch in channels)
            {
                var (ok, msg) = await SendQuickAsync(ch.Provider, ch.EffectiveChatId, text, siteId ?? ch.SiteId, photoUrl);
                results.Add(new ChannelBroadcastResultItem
                {
                    ChannelId = ch.Id,
                    Title = ch.Title,
                    Provider = ch.Provider,
                    Target = NormalizeChatId(ch.Provider, ch.EffectiveChatId),
                    Ok = ok,
                    Message = msg
                });
            }

            return results;
        }

        static bool IsEita(string provider)
            => string.Equals(provider, MessagingProviders.Eita, StringComparison.OrdinalIgnoreCase);

        static string NormalizeChatId(string provider, string chatId)
        {
            if (string.IsNullOrWhiteSpace(chatId)) return "";
            var t = chatId.Trim();
            if (IsEita(provider))
            {
                // ایتا: عدد یا یوزرنیم بدون اجبار @
                if (t.StartsWith("@")) return t.TrimStart('@');
                return t;
            }
            if (t.StartsWith("@")) return t;
            if (long.TryParse(t, out _)) return t;
            return "@" + t;
        }

        async Task<(bool Ok, string ChatId, string Error)> GetChatAsync(BotCredentials creds, string chatId)
        {
            try
            {
                var url = $"{creds.ApiBaseUrl.TrimEnd('/')}/bot{creds.BotToken}/getChat?chat_id={Uri.EscapeDataString(chatId)}";
                var resp = await _http.GetAsync(url);
                var body = await resp.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;
                if (!root.TryGetProperty("ok", out var okEl) || !okEl.GetBoolean())
                {
                    var desc = root.TryGetProperty("description", out var d) ? d.GetString() : body;
                    return (false, null, desc);
                }
                var result = root.GetProperty("result");
                if (result.TryGetProperty("id", out var idEl))
                {
                    var id = idEl.ValueKind == JsonValueKind.Number
                        ? idEl.GetInt64().ToString()
                        : idEl.GetRawText().Trim('"');
                    return (true, id, null);
                }
                return (true, chatId, null);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "getChat failed");
                return (false, null, ex.Message);
            }
        }

        async Task<(bool Ok, string Error)> SendMessageAsync(BotCredentials creds, string chatId, string text)
        {
            try
            {
                object chatIdValue = long.TryParse(chatId, out var n) ? n : (object)chatId;
                var apiBase = (creds.ApiBaseUrl ?? "").Trim().TrimEnd('/');
                string url;
                if (IsEita(creds.Provider))
                {
                    if (string.IsNullOrWhiteSpace(apiBase))
                        apiBase = "https://eitaayar.ir/api";
                    url = $"{apiBase}/{creds.BotToken}/sendMessage";
                }
                else
                {
                    url = $"{apiBase}/bot{creds.BotToken}/sendMessage";
                }

                var payload = new { chat_id = chatIdValue, text, parse_mode = "HTML" };
                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                var resp = await _http.PostAsync(url, content);
                var body = await resp.Content.ReadAsStringAsync();
                return ParseApiOk(body, resp.IsSuccessStatusCode);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Channel sendMessage failed for {Provider}", creds?.Provider);
                return (false, ex.Message);
            }
        }

        async Task<(bool Ok, string Error)> SendPhotoAsync(BotCredentials creds, string chatId, string photoUrl, string caption)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(photoUrl) ||
                    !(photoUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                      || photoUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase)))
                    return (false, "آدرس تصویر باید با http یا https شروع شود.");

                object chatIdValue = long.TryParse(chatId, out var n) ? n : (object)chatId;
                var apiBase = (creds.ApiBaseUrl ?? "").Trim().TrimEnd('/');
                string url;
                if (IsEita(creds.Provider))
                {
                    if (string.IsNullOrWhiteSpace(apiBase))
                        apiBase = "https://eitaayar.ir/api";
                    url = $"{apiBase}/{creds.BotToken}/sendPhoto";
                }
                else
                {
                    url = $"{apiBase}/bot{creds.BotToken}/sendPhoto";
                }

                object payload = string.IsNullOrWhiteSpace(caption)
                    ? new { chat_id = chatIdValue, photo = photoUrl }
                    : new { chat_id = chatIdValue, photo = photoUrl, caption, parse_mode = "HTML" };

                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                var resp = await _http.PostAsync(url, content);
                var body = await resp.Content.ReadAsStringAsync();
                return ParseApiOk(body, resp.IsSuccessStatusCode);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Channel sendPhoto failed for {Provider}", creds?.Provider);
                return (false, ex.Message);
            }
        }

        static (bool Ok, string Error) ParseApiOk(string body, bool httpOk)
        {
            try
            {
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;
                if (root.TryGetProperty("ok", out var okEl) && okEl.GetBoolean())
                    return (true, null);
                var desc = root.TryGetProperty("description", out var d) ? d.GetString()
                    : root.TryGetProperty("result", out var r) ? r.ToString()
                    : body;
                if (!httpOk && string.IsNullOrWhiteSpace(desc))
                    desc = body;
                return (false, desc ?? body);
            }
            catch
            {
                return (httpOk, httpOk ? null : body);
            }
        }
    }
}
