using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MyShop.Application.Interfaces.Messaging;
using MyShop.Core.Models.Messaging;

namespace MyShop.Web.Services
{
    /// <summary>
    /// کانال تلگرام (Bot API رسمی). منطق کسب‌وکار همان OTP/لینک است؛ فقط Provider و ApiBase فرق دارد.
    /// </summary>
    public class TelegramMessagingChannel : IMessagingChannel
    {
        public string Provider => MessagingProviders.Telegram;

        private readonly HttpClient _httpClient;
        private readonly IBotCredentialsResolver _resolver;
        private readonly ILogger<TelegramMessagingChannel> _logger;

        public TelegramMessagingChannel(
            HttpClient httpClient,
            IBotCredentialsResolver resolver,
            ILogger<TelegramMessagingChannel> logger)
        {
            _httpClient = httpClient;
            _resolver = resolver;
            _logger = logger;
        }

        public async Task<(bool Ok, string Error)> SendTextAsync(string chatId, string text)
        {
            var creds = BotExecutionContext.Current
                ?? await _resolver.ResolveAsync(MessagingProviders.Telegram);
            if (creds == null || !creds.HasToken)
                return (false, "BotToken تلگرام برای این سایت تنظیم نشده است.");
            if (string.IsNullOrWhiteSpace(chatId))
                return (false, "ChatId خالی است.");

            try
            {
                object chatIdValue = long.TryParse(chatId.Trim(), out var n) ? n : (object)chatId.Trim();
                var apiBase = string.IsNullOrWhiteSpace(creds.ApiBaseUrl) ? "https://api.telegram.org" : creds.ApiBaseUrl.TrimEnd('/');
                var url = $"{apiBase}/bot{creds.BotToken}/sendMessage";
                var payload = new
                {
                    chat_id = chatIdValue,
                    text = text,
                    parse_mode = "HTML"
                };
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(url, content);
                var body = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                    return (true, null);

                try
                {
                    using var doc = JsonDocument.Parse(body);
                    if (doc.RootElement.TryGetProperty("description", out var d))
                        return (false, d.GetString() ?? body);
                }
                catch { /* ignore */ }

                return (false, string.IsNullOrWhiteSpace(body) ? $"HTTP {(int)response.StatusCode}" : body);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "TelegramMessagingChannel.SendText failed");
                return (false, ex.Message);
            }
        }
    }
}
