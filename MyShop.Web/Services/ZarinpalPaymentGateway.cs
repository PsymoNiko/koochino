using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MyShop.Application.Interfaces.Commerce;
using MyShop.Application.Interfaces.Security;
using MyShop.Application.Services.Security;
using MyShop.Core.Interfaces;

namespace MyShop.Web.Services
{
    /// <summary>درگاه زرین‌پال (API v4). مبلغ به ریال.</summary>
    public class ZarinpalPaymentGateway : IPaymentGateway
    {
        public string Provider => "Zarinpal";

        private readonly HttpClient _http;
        private readonly IConfiguration _config;
        private readonly ILogger<ZarinpalPaymentGateway> _logger;
        private readonly IModuleConfigService _moduleConfig;
        private readonly ICurrentTenant _tenant;

        public ZarinpalPaymentGateway(
            HttpClient http,
            IConfiguration config,
            ILogger<ZarinpalPaymentGateway> logger = null,
            IModuleConfigService moduleConfig = null,
            ICurrentTenant tenant = null)
        {
            _http = http;
            _config = config;
            _logger = logger;
            _moduleConfig = moduleConfig;
            _tenant = tenant;
        }

        async Task<(string MerchantId, bool Sandbox)> ResolveAsync()
        {
            var siteId = ModuleConfigRead.SiteIdOf(_tenant);
            var merchant = await ModuleConfigRead.TryGetAsync(_moduleConfig, siteId, "Payment", "Zarinpal.MerchantId", true)
                           ?? _config["Zarinpal:MerchantId"] ?? "";
            var sandboxRaw = await ModuleConfigRead.TryGetAsync(_moduleConfig, siteId, "Payment", "Zarinpal.Sandbox", false)
                             ?? _config["Zarinpal:Sandbox"];
            var sandbox = string.Equals(sandboxRaw, "true", StringComparison.OrdinalIgnoreCase);
            return (merchant, sandbox);
        }

        public async Task<(bool Ok, string Authority, string RedirectUrl, string Error)> StartPaymentAsync(
            long amountRial, string description, string callbackUrl, string orderNumber)
        {
            var (merchantId, sandbox) = await ResolveAsync();
            if (string.IsNullOrWhiteSpace(merchantId) || merchantId.Contains("YOUR_", StringComparison.OrdinalIgnoreCase))
                return (false, null, null, "MerchantId زرین‌پال در appsettings تنظیم نشده است.");

            var requestUrl = sandbox
                ? "https://sandbox.zarinpal.com/pg/v4/payment/request.json"
                : "https://api.zarinpal.com/pg/v4/payment/request.json";
            var startPayBase = sandbox
                ? "https://sandbox.zarinpal.com/pg/StartPay/"
                : "https://www.zarinpal.com/pg/StartPay/";

            try
            {
                var payload = new
                {
                    merchant_id = merchantId,
                    amount = amountRial,
                    description = description ?? ("سفارش " + orderNumber),
                    callback_url = callbackUrl,
                    metadata = new { order_id = orderNumber }
                };
                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                var resp = await _http.PostAsync(requestUrl, content);
                var body = await resp.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;
                if (root.TryGetProperty("data", out var data)
                    && data.ValueKind == JsonValueKind.Object
                    && data.TryGetProperty("authority", out var authEl))
                {
                    var authority = authEl.GetString();
                    if (!string.IsNullOrWhiteSpace(authority))
                        return (true, authority, startPayBase + authority, null);
                }
                var err = root.TryGetProperty("errors", out var errors) ? errors.ToString() : body;
                return (false, null, null, err);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Zarinpal StartPayment failed");
                return (false, null, null, ex.Message);
            }
        }

        public async Task<(bool Ok, string RefId, string Error)> VerifyAsync(string authority, long amountRial)
        {
            var (merchantId, sandbox) = await ResolveAsync();
            if (string.IsNullOrWhiteSpace(merchantId))
                return (false, null, "MerchantId خالی است.");
            var verifyUrl = sandbox
                ? "https://sandbox.zarinpal.com/pg/v4/payment/verify.json"
                : "https://api.zarinpal.com/pg/v4/payment/verify.json";
            try
            {
                var payload = new { merchant_id = merchantId, amount = amountRial, authority };
                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                var resp = await _http.PostAsync(verifyUrl, content);
                var body = await resp.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;
                if (root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Object)
                {
                    var code = data.TryGetProperty("code", out var c) ? c.GetInt32() : 0;
                    if (code == 100 || code == 101)
                    {
                        var refId = data.TryGetProperty("ref_id", out var r)
                            ? r.ToString()
                            : authority;
                        return (true, refId, null);
                    }
                }
                var err = root.TryGetProperty("errors", out var errors) ? errors.ToString() : body;
                return (false, null, err);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Zarinpal Verify failed");
                return (false, null, ex.Message);
            }
        }
    }
}
