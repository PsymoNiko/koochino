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
    /// <summary>کانال ارسال از طریق بات بله — توکن از IBotCredentialsResolver / SiteBots.</summary>
    public class BaleMessagingChannel : IMessagingChannel
    {
        public string Provider => MessagingProviders.Bale;

        private readonly HttpClient _httpClient;
        private readonly IBotCredentialsResolver _resolver;
        private readonly ILogger<BaleMessagingChannel> _logger;

        public BaleMessagingChannel(
            HttpClient httpClient,
            IBotCredentialsResolver resolver,
            ILogger<BaleMessagingChannel> logger)
        {
            _httpClient = httpClient;
            _resolver = resolver;
            _logger = logger;
        }

        public async Task<(bool Ok, string Error)> SendTextAsync(string chatId, string text)
        {
            var creds = BotExecutionContext.Current
                ?? await _resolver.ResolveAsync(MessagingProviders.Bale);
            if (creds == null || !creds.HasToken)
                return (false, "BotToken بله برای این سایت تنظیم نشده است.");
            if (string.IsNullOrWhiteSpace(chatId))
                return (false, "ChatId خالی است.");

            try
            {
                object chatIdValue = long.TryParse(chatId.Trim(), out var n) ? n : (object)chatId.Trim();
                var apiBase = string.IsNullOrWhiteSpace(creds.ApiBaseUrl) ? "https://tapi.bale.ai" : creds.ApiBaseUrl.TrimEnd('/');
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
                _logger.LogWarning(ex, "BaleMessagingChannel.SendText failed");
                return (false, ex.Message);
            }
        }
    }
}
