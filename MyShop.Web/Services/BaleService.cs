using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MyShop.Application.Interfaces;
using MyShop.Application.Interfaces.Messaging;
using MyShop.Application.Services.Messaging;
using MyShop.Application.Interfaces.SupportChat;
using MyShop.Core.Models.Messaging;

namespace MyShop.Web.Services
{
    /// <summary>
    /// پاسخ خطای API بله: ok=false همراه با description و error_code.
    /// </summary>
    public class BaleApiResponse
    {
        public bool Ok { get; set; }
        public string? Description { get; set; }
        public int? Error_Code { get; set; }
    }

    public class BaleService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly IBaleBotFeedbackRepository? _feedbackRepository;
        private readonly IUserMessagingLinkRepository? _messagingLinks;
        private readonly IMessagingOtpService? _otpService;
        private readonly IMessagingSendLogRepository? _sendLogRepository;
        private readonly IBotMenuItemRepository? _botMenu;
        private readonly IUserRepository? _users;
        private readonly IBotCredentialsResolver? _botResolver;
        private readonly ILogger<BaleService>? _logger;
        private readonly ISupportOmnichannelService? _supportOmni;
        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

        public BaleService(
            HttpClient httpClient,
            IConfiguration configuration,
            IBaleBotFeedbackRepository? feedbackRepository = null,
            IUserMessagingLinkRepository? messagingLinks = null,
            IMessagingOtpService? otpService = null,
            IMessagingSendLogRepository? sendLogRepository = null,
            IBotMenuItemRepository? botMenu = null,
            IUserRepository? users = null,
            IBotCredentialsResolver? botResolver = null,
            ILogger<BaleService>? logger = null,
            ISupportOmnichannelService? supportOmni = null)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _feedbackRepository = feedbackRepository;
            _messagingLinks = messagingLinks;
            _otpService = otpService;
            _sendLogRepository = sendLogRepository;
            _botMenu = botMenu;
            _users = users;
            _botResolver = botResolver;
            _logger = logger;
            _supportOmni = supportOmni;
        }

        /// <summary>
        /// متن خطای کامل (شامل InnerException) برای نمایش یا لاگ.
        /// </summary>
        private static string GetFullExceptionMessage(Exception ex)
        {
            var msg = ex.Message;
            var inner = ex.InnerException;
            while (inner != null)
            {
                msg += " | Inner: " + inner.Message;
                inner = inner.InnerException;
            }
            return msg;
        }

        /// <summary>
        /// نرمال کردن ChatId: برای کانال با یوزرنیم باید با @ باشد (مثلاً @channel).
        /// اگر عدد نباشد و @ نداشته باشد، @ اضافه می‌شود.
        /// </summary>
        private static string NormalizeChatId(string? chatId)
        {
            if (string.IsNullOrWhiteSpace(chatId)) return chatId ?? "";
            var t = chatId.Trim();
            if (t.StartsWith("@")) return t;
            // شناسه عددی (گروه/کاربر) یا عدد منفی
            if (long.TryParse(t, out _)) return t;
            return "@" + t;
        }

        /// <summary>Provider فعلی از context وب‌هوک (Bale / Telegram / ...).</summary>
        private string CurrentProvider =>
            !string.IsNullOrWhiteSpace(BotExecutionContext.Current?.Provider)
                ? BotExecutionContext.Current.Provider
                : MessagingProviders.Bale;

        private string ProviderLabel =>
            CurrentProvider.Equals(MessagingProviders.Telegram, StringComparison.OrdinalIgnoreCase) ? "تلگرام"
            : CurrentProvider.Equals(MessagingProviders.Bale, StringComparison.OrdinalIgnoreCase) ? "بله"
            : CurrentProvider;

        private BotCredentials ResolveCreds()
        {
            if (BotExecutionContext.Current != null)
                return BotExecutionContext.Current;
            if (_botResolver != null)
                return _botResolver.ResolveAsync(MessagingProviders.Bale).GetAwaiter().GetResult();
            return new BotCredentials
            {
                Provider = MessagingProviders.Bale,
                BotToken = (_configuration["Bale:BotToken"] ?? "").Trim(),
                ApiBaseUrl = (_configuration["Bale:ApiBaseUrl"] ?? "https://tapi.bale.ai").Trim().TrimEnd('/'),
                ChatId = _configuration["Bale:ChatId"],
                BotLink = _configuration["Bale:BotLink"],
                WebhookSecret = _configuration["Bale:WebhookSecret"],
                IsActive = true
            };
        }

        private (string? botToken, string? chatId) GetConfig()
        {
            var c = ResolveCreds();
            var chatId = string.IsNullOrWhiteSpace(c.ChatId) ? null : NormalizeChatId(c.ChatId);
            return (c.HasToken ? c.BotToken : null, chatId);
        }

        /// <summary>
        /// پایه API بله. روی هاست اشتراکی اگر TLS مستقیم کار نکرد،
        /// در appsettings بگذارید Bale:ApiBaseUrl = آدرس Cloudflare Worker پروکسی.
        /// </summary>
        private string GetApiBase()
        {
            var c = ResolveCreds();
            if (!string.IsNullOrWhiteSpace(c.ApiBaseUrl))
                return c.ApiBaseUrl.Trim().TrimEnd('/');
            var baseUrl = _configuration["Bale:ApiBaseUrl"];
            if (string.IsNullOrWhiteSpace(baseUrl))
                baseUrl = "https://tapi.bale.ai";
            return baseUrl.Trim().TrimEnd('/');
        }

        private string BotApi(string method)
        {
            var (botToken, _) = GetConfig();
            return $"{GetApiBase()}/bot{botToken}/{method.TrimStart('/')}";
        }

        /// <summary>
        /// تست توکن و دریافت اطلاعات ربات (getMe).
        /// </summary>
        public async Task<(bool Success, string BotNameOrError)> GetMeAsync()
        {
            var (botToken, _) = GetConfig();
            if (string.IsNullOrEmpty(botToken))
                return (false, "BotToken تنظیم نشده است.");
            try
            {
                var url = BotApi("getMe");
                var response = await _httpClient.GetAsync(url);
                var body = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;
                if (root.TryGetProperty("ok", out var okEl) && okEl.GetBoolean())
                {
                    if (root.TryGetProperty("result", out var result))
                    {
                        if (result.TryGetProperty("username", out var usernameEl))
                            return (true, "@" + usernameEl.GetString());
                        if (result.TryGetProperty("first_name", out var fn))
                            return (true, fn.GetString() ?? "ربات");
                    }
                    return (true, "ربات معتبر");
                }
                if (root.TryGetProperty("description", out var descEl))
                    return (false, descEl.GetString() ?? body);
                return (false, body);
            }
            catch (Exception ex)
            {
                var fullError = GetFullExceptionMessage(ex);
                _logger?.LogWarning(ex, "Bale GetMe failed: {Error}", fullError);
                return (false, fullError);
            }
        }

        /// <summary>
        /// وضعیت وب‌هوک فعلی ربات (getWebhookInfo).
        /// </summary>
        public async Task<(bool Success, string Url, string? LastErrorMessage, string? Error)> GetWebhookInfoAsync()
        {
            var (botToken, _) = GetConfig();
            if (string.IsNullOrEmpty(botToken))
                return (false, "", null, "BotToken تنظیم نشده است.");
            try
            {
                var url = BotApi("getWebhookInfo");
                var response = await _httpClient.GetAsync(url);
                var body = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;
                if (!root.TryGetProperty("ok", out var okEl) || !okEl.GetBoolean())
                {
                    var desc = root.TryGetProperty("description", out var d) ? d.GetString() : body;
                    return (false, "", null, desc);
                }

                var result = root.GetProperty("result");
                var hookUrl = result.TryGetProperty("url", out var u) ? (u.GetString() ?? "") : "";
                string? lastErr = null;
                if (result.TryGetProperty("last_error_message", out var le) && le.ValueKind == JsonValueKind.String)
                    lastErr = le.GetString();
                return (true, hookUrl, lastErr, null);
            }
            catch (Exception ex)
            {
                var fullError = GetFullExceptionMessage(ex);
                _logger?.LogWarning(ex, "Bale GetWebhookInfo failed: {Error}", fullError);
                return (false, "", null, fullError);
            }
        }

        /// <summary>
        /// لینک GET برای ثبت وب‌هوک از مرورگر کاربر (TLS از کامپیوتر ادمین، نه سرور IIS).
        /// </summary>
        public string? GetBrowserSetWebhookUrl(string webhookUrl)
        {
            var (botToken, _) = GetConfig();
            if (string.IsNullOrEmpty(botToken) || string.IsNullOrWhiteSpace(webhookUrl))
                return null;
            return null;
        }

        /// <summary>
        /// لینک GET برای مشاهده وضعیت وب‌هوک از مرورگر.
        /// </summary>
        public string? GetBrowserGetWebhookInfoUrl()
        {
            var (botToken, _) = GetConfig();
            if (string.IsNullOrEmpty(botToken)) return null;
            return null;
        }

        /// <summary>ثبت وب‌هوک از سرور با توکن مشخص — توکن به مرورگر نمی‌رود.</summary>
        public async Task<(bool Success, string Message)> SetWebhookWithCredentialsAsync(
            string apiBase, string botToken, string webhookUrl, string secretToken = null)
        {
            if (string.IsNullOrWhiteSpace(botToken))
                return (false, "BotToken تنظیم نشده است.");
            if (string.IsNullOrWhiteSpace(webhookUrl) || !webhookUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                return (false, "آدرس وب‌هوک باید با https:// شروع شود.");
            return await PostBotJsonAsync(apiBase, botToken, "setWebhook", BuildSetWebhookPayload(webhookUrl, secretToken));
        }

        public async Task<(bool Success, string Message)> GetWebhookInfoWithCredentialsAsync(string apiBase, string botToken)
        {
            if (string.IsNullOrWhiteSpace(botToken))
                return (false, "BotToken تنظیم نشده است.");
            var baseUrl = string.IsNullOrWhiteSpace(apiBase) ? GetApiBase() : apiBase.Trim().TrimEnd('/');
            try
            {
                var url = $"{baseUrl}/bot{botToken.Trim()}/getWebhookInfo";
                var response = await _httpClient.GetAsync(url);
                var body = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;
                if (root.TryGetProperty("ok", out var okEl) && okEl.GetBoolean())
                {
                    var urlSet = "";
                    if (root.TryGetProperty("result", out var result) && result.TryGetProperty("url", out var u))
                        urlSet = u.GetString() ?? "";
                    return (true, string.IsNullOrWhiteSpace(urlSet) ? "وب‌هوک ثبت نشده." : urlSet);
                }
                var err = root.TryGetProperty("description", out var de) ? de.GetString() : body;
                return (false, err ?? body);
            }
            catch (Exception ex)
            {
                return (false, GetFullExceptionMessage(ex));
            }
        }

        static object BuildSetWebhookPayload(string webhookUrl, string secretToken)
        {
            if (string.IsNullOrWhiteSpace(secretToken))
                return new { url = webhookUrl.Trim() };
            return new { url = webhookUrl.Trim(), secret_token = secretToken.Trim() };
        }

        async Task<(bool Success, string Message)> PostBotJsonAsync(string apiBase, string botToken, string method, object payload)
        {
            if (string.IsNullOrWhiteSpace(botToken))
                return (false, "BotToken تنظیم نشده است.");
            if (payload is { } && method == "setWebhook")
            {
                /* url check in caller */
            }
            var baseUrl = string.IsNullOrWhiteSpace(apiBase) ? GetApiBase() : apiBase.Trim().TrimEnd('/');
            try
            {
                var url = $"{baseUrl}/bot{botToken.Trim()}/{method.TrimStart('/')}";
                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(url, content);
                var body = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;
                if (root.TryGetProperty("ok", out var okEl) && okEl.GetBoolean())
                {
                    var desc = root.TryGetProperty("description", out var d) ? d.GetString() : null;
                    return (true, string.IsNullOrWhiteSpace(desc) ? "انجام شد." : desc!);
                }
                var err = root.TryGetProperty("description", out var de) ? de.GetString() : body;
                return (false, err ?? body);
            }
            catch (Exception ex)
            {
                return (false, GetFullExceptionMessage(ex));
            }
        }

        /// <summary>
        /// ثبت وب‌هوک روی URL عمومی سایت (setWebhook).
        /// </summary>
        public async Task<(bool Success, string Message)> SetWebhookAsync(string webhookUrl)
        {
            var c = ResolveCreds();
            var token = c.HasToken ? c.BotToken : null;
            var (ok, msg) = await SetWebhookWithCredentialsAsync(GetApiBase(), token, webhookUrl, c.WebhookSecret);
            if (ok) return (true, msg);
            if (msg != null && (msg.Contains("HandshakeFailure", StringComparison.OrdinalIgnoreCase)
                || msg.Contains("SSL", StringComparison.OrdinalIgnoreCase)
                || msg.Contains("TLS", StringComparison.OrdinalIgnoreCase)
                || msg.Contains("12175")))
            {
                return (false, msg + " — خروج HTTPS از هاست اشتراکی به بله قطع است. ApiBaseUrl را روی Worker بگذارید.");
            }
            return (false, msg);
        }

        /// <summary>
        /// ارسال پیام با برگرداندن متن خطای API در صورت شکست.
        /// </summary>
        public async Task<(bool Success, string? ErrorMessage)> SendMessageWithDetailAsync(string message)
        {
            var (botToken, chatId) = GetConfig();
            if (string.IsNullOrEmpty(botToken) || string.IsNullOrEmpty(chatId))
                return (false, "BotToken یا ChatId در appsettings تنظیم نشده است.");
            try
            {
                var url = BotApi("sendMessage");
                var payload = new
                {
                    chat_id = chatId,
                    text = message,
                    parse_mode = "HTML"
                };
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(url, content);
                var body = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                    return (true, null);
                var api = JsonSerializer.Deserialize<BaleApiResponse>(body, JsonOptions);
                var err = api?.Description ?? body;
                if (!string.IsNullOrWhiteSpace(err))
                    return (false, err);
                return (false, $"HTTP {(int)response.StatusCode}");
            }
            catch (Exception ex)
            {
                var fullError = GetFullExceptionMessage(ex);
                _logger?.LogWarning(ex, "Bale SendMessage failed: {Error}", fullError);
                return (false, fullError);
            }
        }

        public async Task<bool> SendMessageAsync(string message, string? imageUrl = null)
        {
            var (success, _) = await SendMessageWithDetailAsync(message);
            return success;
        }

        public async Task<bool> SendPostAsync(string title, string content, string? summary = null, string? imageUrl = null)
        {
            try
            {
                var message = $"<b>{title}</b>\n\n";
                
                if (!string.IsNullOrEmpty(summary))
                {
                    message += $"{summary}\n\n";
                }
                
                message += content;

                if (!string.IsNullOrEmpty(imageUrl) && imageUrl.StartsWith("http"))
                {
                    // اگر تصویر دارید، می‌توانید از sendPhoto استفاده کنید
                    return await SendPhotoAsync(imageUrl, message);
                }

                return await SendMessageAsync(message);
            }
            catch
            {
                return false;
            }
        }

        private async Task<bool> SendPhotoAsync(string photoUrl, string caption)
        {
            var (botToken, chatId) = GetConfig();
            if (string.IsNullOrEmpty(botToken) || string.IsNullOrEmpty(chatId))
                return false;
            try
            {
                var url = BotApi("sendPhoto");
                var payload = new
                {
                    chat_id = chatId,
                    photo = photoUrl,
                    caption = caption,
                    parse_mode = "HTML"
                };
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(url, content);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// منوی اصلی بات پس از /start — گزینه‌ها از دیتابیس (پنل ادمین).
        /// </summary>
        public async Task<bool> SendStartMenuToChatAsync(object chatId)
        {
            var (botToken, _) = GetConfig();
            if (string.IsNullOrEmpty(botToken))
                return false;
            try
            {
                var items = _botMenu != null
                    ? await _botMenu.ListAsync(CurrentProvider, onlyActive: true)
                    : new List<BotMenuItem>();

                if ((items == null || items.Count == 0) &&
                    !CurrentProvider.Equals(MessagingProviders.Bale, StringComparison.OrdinalIgnoreCase) &&
                    _botMenu != null)
                {
                    // اگر منوی Provider خالی بود، منوی بله را به‌عنوان قالب مشترک بگیر
                    items = await _botMenu.ListAsync(MessagingProviders.Bale, onlyActive: true);
                }

                if (items == null || items.Count == 0)
                {
                    // fallback اگر جدول خالی/اجرا نشده باشد
                    items = new List<BotMenuItem>
                    {
                        new() { Id = 0, Title = "1 - دریافت کد فعال‌سازی", ActionType = BotMenuActionTypes.SendOtp },
                        new() { Id = -1, Title = "2 - اتصال شماره موبایل", ActionType = BotMenuActionTypes.AskPhone }
                    };
                }

                var rows = new List<object[]>();
                foreach (var item in items)
                {
                    var cb = item.Id > 0 ? $"mi:{item.Id}" : FallbackCallback(item.ActionType);
                    rows.Add(new object[] { new { text = item.Title, callback_data = cb } });
                }

                var url = BotApi("sendMessage");
                var payload = new
                {
                    chat_id = chatId,
                    text = "از منوی زیر یک گزینه را انتخاب کنید:",
                    reply_markup = new { inline_keyboard = rows }
                };
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(url, content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "SendStartMenuToChatAsync failed");
                return false;
            }
        }

        private static string FallbackCallback(string actionType) => actionType switch
        {
            BotMenuActionTypes.SendOtp => "get_activation_code",
            BotMenuActionTypes.AskPhone => "link_phone",
            BotMenuActionTypes.Feedback => "good",
            _ => "link_phone"
        };

        /// <summary>
        /// پاسخ به callback query (برای بستن loading روی دکمه).
        /// </summary>
        public async Task AnswerCallbackQueryAsync(string callbackQueryId, string? text = null)
        {
            var (botToken, _) = GetConfig();
            if (string.IsNullOrEmpty(botToken)) return;
            try
            {
                var url = BotApi("answerCallbackQuery");
                string json = string.IsNullOrEmpty(text)
                    ? JsonSerializer.Serialize(new { callback_query_id = callbackQueryId })
                    : JsonSerializer.Serialize(new { callback_query_id = callbackQueryId, text = text });
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                await _httpClient.PostAsync(url, content);
            }
            catch
            {
                // ignore
            }
        }

        /// <summary>
        /// ارسال پیام به یک چت مشخص (مشتری).
        /// </summary>
        public async Task<(bool Success, string? ErrorMessage)> SendToChatAsync(string chatId, string message)
        {
            return await SendToChatAsync(chatId, message, replyMarkup: null);
        }

        /// <summary>ارسال پیام با reply_markup اختیاری (مثلاً کیبورد اشتراک مخاطب).</summary>
        public async Task<(bool Success, string? ErrorMessage)> SendToChatAsync(string chatId, string message, object replyMarkup)
        {
            var (botToken, _) = GetConfig();
            if (string.IsNullOrEmpty(botToken))
                return (false, "BotToken تنظیم نشده است.");
            if (string.IsNullOrWhiteSpace(chatId))
                return (false, "ChatId خالی است.");
            try
            {
                object chatIdValue = long.TryParse(chatId.Trim(), out var n) ? n : (object)chatId.Trim();
                var url = BotApi("sendMessage");
                object payload = replyMarkup == null
                    ? new { chat_id = chatIdValue, text = message, parse_mode = "HTML" }
                    : new { chat_id = chatIdValue, text = message, parse_mode = "HTML", reply_markup = replyMarkup };
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(url, content);
                var body = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                {
                    await LogOutboundAsync(chatId, message, true, null);
                    return (true, null);
                }
                var api = JsonSerializer.Deserialize<BaleApiResponse>(body, JsonOptions);
                var err = api?.Description ?? body;
                await LogOutboundAsync(chatId, message, false, err);
                return (false, err);
            }
            catch (Exception ex)
            {
                var fullError = GetFullExceptionMessage(ex);
                _logger?.LogWarning(ex, "Bale SendToChat failed: {Error}", fullError);
                await LogOutboundAsync(chatId, message, false, fullError);
                return (false, fullError);
            }
        }

        /// <summary>کیبورد یک‌بارمصرف برای اشتراک مخاطب (Telegram/Bale سازگار).</summary>
        public async Task<(bool Success, string? ErrorMessage)> SendShareContactPromptAsync(string chatId, string promptText = null)
        {
            var text = string.IsNullOrWhiteSpace(promptText)
                ? "برای اتصال شماره، روی دکمه زیر بزنید و مخاطب خودتان را به اشتراک بگذارید."
                : promptText.Trim();
            var markup = new
            {
                keyboard = new[]
                {
                    new[]
                    {
                        new { text = "📱 اشتراک‌گذاری مخاطب من", request_contact = true }
                    }
                },
                resize_keyboard = true,
                one_time_keyboard = true
            };
            return await SendToChatAsync(chatId, text, markup);
        }

        private async Task LogOutboundAsync(string chatId, string message, bool success, string error)
        {
            try
            {
                // لاگ از طریق سرویس OTP (اگر ثبت شده) یا مستقیم از DI در ProcessUpdate کافی است؛
                // اینجا فقط وقتی IMessagingSendLogRepository تزریق شده باشد.
                var logs = _sendLogRepository;
                if (logs == null) return;
                string phone = null;
                if (_messagingLinks != null && !string.IsNullOrWhiteSpace(chatId))
                {
                    var link = await _messagingLinks.GetByChatAsync(CurrentProvider, chatId);
                    phone = link?.PhoneNumber;
                }
                await logs.InsertAsync(new MessagingSendLog
                {
                    Provider = CurrentProvider,
                    PhoneNumber = phone,
                    ChatId = chatId,
                    MessageType = MessagingMessageTypes.BotReply,
                    MessageText = message ?? "",
                    Success = success,
                    ErrorMessage = success ? null : error
                });
            }
            catch { /* ignore */ }
        }

        /// <summary>
        /// پردازش آپدیت وب‌هوک: /start → درخواست شماره؛ شماره → لینک؛ خوبی/بدی → بازخورد.
        /// </summary>
        public Task ProcessUpdateAsync(string jsonBody)
            => ProcessUpdateAsync(jsonBody, null);

        public async Task ProcessUpdateAsync(string jsonBody, BotCredentials credentials)
        {
            if (string.IsNullOrWhiteSpace(jsonBody)) return;
            var previous = BotExecutionContext.Current;
            try
            {
                if (credentials != null)
                    BotExecutionContext.Current = credentials;
                else if (_botResolver != null)
                    BotExecutionContext.Current = await _botResolver.ResolveAsync(MessagingProviders.Bale);

                await ProcessUpdateCoreAsync(jsonBody);
            }
            finally
            {
                BotExecutionContext.Current = previous;
            }
        }

        async Task ProcessUpdateCoreAsync(string jsonBody)
        {
            try
            {
                using var doc = JsonDocument.Parse(jsonBody);
                var root = doc.RootElement;

                if (root.TryGetProperty("message", out var messageEl))
                {
                    // فقط چت خصوصی بات — پست کانال/گروه نباید وارد فلو شماره شود
                    var chatType = messageEl.TryGetProperty("chat", out var chatTypeEl)
                        && chatTypeEl.TryGetProperty("type", out var typeEl)
                        ? typeEl.GetString()
                        : null;
                    if (!string.Equals(chatType, "private", StringComparison.OrdinalIgnoreCase))
                        return;

                    // پیام خودِ بات (مثلاً اکو پست کانال) را نادیده بگیر تا حلقهٔ اتصال شماره ایجاد نشود
                    if (messageEl.TryGetProperty("from", out var fromBotEl)
                        && fromBotEl.TryGetProperty("is_bot", out var isBotEl)
                        && isBotEl.ValueKind == JsonValueKind.True)
                        return;

                    var chatId = messageEl.TryGetProperty("chat", out var chatEl) && chatEl.TryGetProperty("id", out var idEl)
                        ? idEl.GetRawText().Trim('"')
                        : null;

                    string? externalUserId = null, username = null, firstName = null;
                    if (messageEl.TryGetProperty("from", out var fromEl))
                    {
                        if (fromEl.TryGetProperty("id", out var uidEl))
                            externalUserId = uidEl.GetRawText().Trim('"');
                        if (fromEl.TryGetProperty("username", out var uEl))
                            username = uEl.GetString();
                        if (fromEl.TryGetProperty("first_name", out var fnEl))
                            firstName = fnEl.GetString();
                    }

                    var text = messageEl.TryGetProperty("text", out var t) ? t.GetString()?.Trim() : null;

                    // اشتراک مخاطب (contact) — ترجیحاً شماره اکانت بله
                    string? contactPhone = null;
                    string? contactOwnerId = null;
                    if (messageEl.TryGetProperty("contact", out var contactEl) &&
                        contactEl.TryGetProperty("phone_number", out var phoneEl))
                    {
                        contactPhone = phoneEl.GetString();
                        if (contactEl.TryGetProperty("user_id", out var contactUidEl))
                            contactOwnerId = contactUidEl.GetRawText().Trim('"');
                    }

                    if (!string.IsNullOrEmpty(chatId) && string.Equals(text, "/start", StringComparison.OrdinalIgnoreCase))
                    {
                        await SendToChatAsync(chatId,
                            "سلام 👋 به بات خوش آمدید.\nبرای دریافت کد فعال‌سازی ابتدا شماره موبایل خود را متصل کنید (گزینه ۲ یا اشتراک مخاطب خودتان).\nمثال: 09121234567");
                        await SendStartMenuToChatAsync(chatId);
                        var startSite = BotExecutionContext.Current?.SiteId ?? 0;
                        if (_supportOmni != null && startSite > 0 && await _supportOmni.IsEnabledAsync(startSite))
                            await SendToChatAsync(chatId, "برای گفتگو با پشتیبانی انسانی دستور /support را ارسال کنید.");
                        return;
                    }

                    if (!string.IsNullOrEmpty(chatId) && !string.IsNullOrWhiteSpace(text)
                        && (string.Equals(text, "/support", StringComparison.OrdinalIgnoreCase)
                            || string.Equals(text.Trim(), "پشتیبانی", StringComparison.OrdinalIgnoreCase)))
                    {
                        var siteId = BotExecutionContext.Current?.SiteId ?? 0;
                        if (_supportOmni != null && siteId > 0 && await _supportOmni.IsEnabledAsync(siteId))
                        {
                            await _supportOmni.HandleInboundAsync(CurrentProvider, siteId, chatId, firstName ?? username, "سلام، نیاز به پشتیبانی دارم.");
                            return;
                        }
                    }

                    var rawPhone = contactPhone ?? text;
                    var phone = MessagingOtpService.NormalizePhone(rawPhone ?? "");
                    // برای متن آزاد: فقط وقتی کل پیام اساساً یک شماره است (نه جملهٔ طولانی با رقم نمونه)
                    var textLooksLikePhoneOnly = contactPhone != null
                        || (text != null && text.Length <= 16 && phone.Length >= 10 && phone.StartsWith("09"));
                    if (!string.IsNullOrEmpty(chatId) && textLooksLikePhoneOnly && phone.Length >= 10 && phone.StartsWith("09"))
                    {
                        await HandlePhoneLinkAsync(chatId, phone, externalUserId, username, contactPhone != null, contactOwnerId);
                        return;
                    }

                    if (!string.IsNullOrEmpty(chatId) && !string.IsNullOrWhiteSpace(text) && !text.StartsWith("/"))
                    {
                        var siteId = BotExecutionContext.Current?.SiteId ?? 0;
                        if (_supportOmni != null && siteId > 0 && await _supportOmni.ShouldInterceptFreeTextAsync(siteId))
                        {
                            await _supportOmni.HandleInboundAsync(CurrentProvider, siteId, chatId, firstName ?? username, text);
                            return;
                        }
                        await SendToChatAsync(chatId, "شماره موبایل معتبر ارسال کنید (مثال: 09121234567) یا از دکمه اشتراک مخاطبِ خودتان استفاده کنید.");
                    }
                    return;
                }

                // آپدیت پست کانال را عمداً نادیده می‌گیریم
                if (root.TryGetProperty("channel_post", out _))
                    return;

                if (root.TryGetProperty("callback_query", out var cqEl))
                {
                    var callbackId = cqEl.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;
                    var data = cqEl.TryGetProperty("data", out var dataEl) ? dataEl.GetString() : null;
                    if (string.IsNullOrEmpty(callbackId) || string.IsNullOrEmpty(data))
                        return;

                    string? cbChatId = null;
                    if (cqEl.TryGetProperty("message", out var cbMsg)
                        && cbMsg.TryGetProperty("chat", out var cbChat)
                        && cbChat.TryGetProperty("id", out var cbChatIdEl))
                    {
                        cbChatId = cbChatIdEl.GetRawText().Trim('"');
                    }

                    // منوی داینامیک: mi:{id}
                    if (data.StartsWith("mi:", StringComparison.OrdinalIgnoreCase)
                        && int.TryParse(data.Substring(3), out var menuId)
                        && _botMenu != null)
                    {
                        var menuItem = await _botMenu.GetByIdAsync(menuId);
                        if (menuItem != null && menuItem.IsActive)
                        {
                            await ExecuteMenuActionAsync(callbackId, cbChatId, menuItem, cqEl);
                            return;
                        }
                    }

                    // سازگاری با دکمه‌های قدیمی
                    if (data == "get_activation_code")
                    {
                        await HandleGetActivationCodeAsync(callbackId, cbChatId);
                        return;
                    }

                    if (data == "link_phone")
                    {
                        await AnswerCallbackQueryAsync(callbackId, "شماره را ارسال کنید");
                        if (!string.IsNullOrEmpty(cbChatId))
                            await SendToChatAsync(cbChatId,
                                $"لطفاً شماره موبایل <b>خودتان</b> را ارسال کنید.\nبهتر است از اشتراک مخاطب اکانت {ProviderLabel} استفاده کنید.\nمثال: <code>09121234567</code>");
                        return;
                    }

                    if (data == "good" || data == "bad")
                    {
                        await SaveFeedbackAsync(cqEl, data);
                        await AnswerCallbackQueryAsync(callbackId, data == "good" ? "خوبی ثبت شد ✓" : "بدی ثبت شد ✓");
                        if (!string.IsNullOrEmpty(cbChatId))
                            await SendToChatAsync(cbChatId, GetFeedbackThankYouMessage());
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Bale ProcessUpdate failed");
            }
        }

        private async Task ExecuteMenuActionAsync(string callbackId, string? chatId, BotMenuItem item, JsonElement cqEl)
        {
            var action = item.ActionType ?? "";
            if (string.Equals(action, BotMenuActionTypes.SendOtp, StringComparison.OrdinalIgnoreCase))
            {
                await HandleGetActivationCodeAsync(callbackId, chatId, item.ActionPayload);
                return;
            }
            if (string.Equals(action, BotMenuActionTypes.AskPhone, StringComparison.OrdinalIgnoreCase))
            {
                await AnswerCallbackQueryAsync(callbackId, "شماره را ارسال کنید");
                if (!string.IsNullOrEmpty(chatId))
                    await SendToChatAsync(chatId,
                        $"لطفاً شماره موبایل <b>خودتان</b> را ارسال کنید.\nبهتر است از اشتراک مخاطب اکانت {ProviderLabel} استفاده کنید تا شماره اکانت شما دریافت شود.\nمثال: <code>09121234567</code>");
                return;
            }
            if (string.Equals(action, BotMenuActionTypes.ShareContact, StringComparison.OrdinalIgnoreCase))
            {
                await AnswerCallbackQueryAsync(callbackId, "دکمه اشتراک مخاطب");
                if (!string.IsNullOrEmpty(chatId))
                {
                    var prompt = string.IsNullOrWhiteSpace(item.ActionPayload)
                        ? $"برای اتصال شماره به پنل، مخاطب {ProviderLabel} خودتان را به اشتراک بگذارید."
                        : item.ActionPayload.Trim();
                    await SendShareContactPromptAsync(chatId, prompt);
                }
                return;
            }
            if (string.Equals(action, BotMenuActionTypes.ReplyText, StringComparison.OrdinalIgnoreCase))
            {
                await AnswerCallbackQueryAsync(callbackId, "✓");
                if (!string.IsNullOrEmpty(chatId) && !string.IsNullOrWhiteSpace(item.ActionPayload))
                    await SendToChatAsync(chatId, item.ActionPayload);
                return;
            }
            if (string.Equals(action, BotMenuActionTypes.Feedback, StringComparison.OrdinalIgnoreCase))
            {
                var raw = (item.ActionPayload ?? "good").Trim();
                var parts = raw.Split(new[] { '|', '\n' }, 2, StringSplitOptions.RemoveEmptyEntries);
                var fb = parts.Length > 0 ? parts[0].Trim() : "good";
                if (!string.Equals(fb, "good", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(fb, "bad", StringComparison.OrdinalIgnoreCase))
                    fb = "good";
                await SaveFeedbackAsync(cqEl, fb);
                var thankYou = parts.Length > 1 ? parts[1].Trim() : GetFeedbackThankYouMessage();
                await AnswerCallbackQueryAsync(callbackId, "ثبت شد ✓");
                if (!string.IsNullOrEmpty(chatId) && !string.IsNullOrWhiteSpace(thankYou))
                    await SendToChatAsync(chatId, thankYou);
            }
        }

        string GetFeedbackThankYouMessage()
        {
            try
            {
                if (_messagingLinks != null)
                {
                    // خواندن از Settings در صورت وجود
                }
                var fromCfg = _configuration?["Messaging:FeedbackThankYouMessage"];
                if (!string.IsNullOrWhiteSpace(fromCfg))
                    return fromCfg.Trim();

                // تلاش از جدول Settings (بدون وابستگی اضافه)
                var cs = _configuration?.GetConnectionString("DefaultConnection")
                    ?? _configuration?.GetConnectionString("Default");
                if (!string.IsNullOrWhiteSpace(cs))
                {
                    using var conn = new Microsoft.Data.SqlClient.SqlConnection(cs);
                    conn.Open();
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = "SELECT [Value] FROM [Settings] WHERE [Key] = @Key";
                    cmd.Parameters.AddWithValue("@Key", "Messaging:FeedbackThankYouMessage");
                    var val = cmd.ExecuteScalar()?.ToString();
                    if (!string.IsNullOrWhiteSpace(val))
                        return val.Trim();
                }
            }
            catch { /* ignore */ }
            return "پیام شما با موفقیت ارسال شد ✓";
        }

        private async Task SaveFeedbackAsync(JsonElement cqEl, string data)
        {
            long userId = 0;
            string? cbUsername = null, firstName = null, lastName = null;
            if (cqEl.TryGetProperty("from", out var fromEl))
            {
                if (fromEl.TryGetProperty("id", out var uidEl))
                    long.TryParse(uidEl.GetRawText(), out userId);
                if (fromEl.TryGetProperty("username", out var uEl))
                    cbUsername = uEl.GetString();
                if (fromEl.TryGetProperty("first_name", out var fnEl))
                    firstName = fnEl.GetString();
                if (fromEl.TryGetProperty("last_name", out var lnEl))
                    lastName = lnEl.GetString();
            }
            if (userId != 0 && _feedbackRepository != null)
                await _feedbackRepository.AddAsync(userId, cbUsername, firstName, lastName, data);
        }

        private async Task HandlePhoneLinkAsync(
            string chatId,
            string phone,
            string? externalUserId,
            string? username,
            bool fromContact,
            string? contactOwnerId)
        {
            if (_messagingLinks == null)
            {
                await SendToChatAsync(chatId, "سرویس اتصال شماره فعلاً در دسترس نیست.");
                return;
            }

            // اگر مخاطب اشتراک گذاشته شده و متعلق به اکانت دیگری است، رد شود
            if (fromContact
                && !string.IsNullOrWhiteSpace(contactOwnerId)
                && !string.IsNullOrWhiteSpace(externalUserId)
                && !string.Equals(contactOwnerId, externalUserId, StringComparison.OrdinalIgnoreCase))
            {
                await SendToChatAsync(chatId,
                    $"❌ این شماره متعلق به اکانت {ProviderLabel} دیگری است.\nلطفاً فقط شماره <b>خودتان</b> را از طریق اشتراک مخاطب ارسال کنید.");
                return;
            }

            int? siteUserId = null;
            try
            {
                if (_users != null)
                {
                    // جلوگیری از اتصال شماره‌ای که قبلاً برای کاربر دیگری تأیید شده
                    if (await _users.IsPhoneVerifiedByAnotherUserAsync(phone, null))
                    {
                        var existing = await _messagingLinks.GetByPhoneAsync(CurrentProvider, phone);
                        if (existing == null
                            || string.IsNullOrWhiteSpace(existing.ChatId)
                            || !string.Equals(existing.ChatId, chatId, StringComparison.OrdinalIgnoreCase))
                        {
                            await SendToChatAsync(chatId,
                                "❌ این شماره موبایل قبلاً برای یک اکانت دیگر تأیید شده و قابل اتصال مجدد نیست.");
                            return;
                        }
                    }

                    siteUserId = await _users.FindUserIdByPhoneAsync(phone);
                    if (siteUserId.HasValue)
                    {
                        // اگر کاربر سایت قبلاً تأیید شده و لینک چت دیگری دارد
                        if (await _users.IsPhoneVerifiedAsync(siteUserId.Value))
                        {
                            var byPhone = await _messagingLinks.GetByPhoneAsync(CurrentProvider, phone);
                            if (byPhone != null
                                && !string.IsNullOrWhiteSpace(byPhone.ChatId)
                                && !string.Equals(byPhone.ChatId, chatId, StringComparison.OrdinalIgnoreCase))
                            {
                                await SendToChatAsync(chatId,
                                    $"❌ این شماره قبلاً با اکانت {ProviderLabel} دیگری فعال شده است.");
                                return;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Phone link validation failed for {Phone}", phone);
            }

            await _messagingLinks.UpsertAsync(new UserMessagingLink
            {
                Provider = CurrentProvider,
                PhoneNumber = phone,
                ChatId = chatId,
                ExternalUserId = externalUserId,
                Username = username,
                UserId = siteUserId,
                IsVerified = true
            });

            var tip = fromContact
                ? $"شماره اکانت {ProviderLabel} شما دریافت شد."
                : "اگر این شماره متعلق به شما نیست، دوباره با اشتراک مخاطبِ خودتان وصل کنید.";

            await SendToChatAsync(chatId,
                $"✅ شماره {phone} با موفقیت به بات متصل شد.\n{tip}\nاز این پس کدهای تأیید و پیام‌ها به همین چت ارسال می‌شود.");
        }

        private async Task HandleGetActivationCodeAsync(string callbackId, string? chatId, string purpose = null)
        {
            purpose = string.IsNullOrWhiteSpace(purpose) ? MessagingPurposes.Register : purpose.Trim();
            if (string.IsNullOrEmpty(chatId))
            {
                await AnswerCallbackQueryAsync(callbackId, "خطا در شناسایی چت");
                return;
            }

            if (_messagingLinks == null || _otpService == null)
            {
                await AnswerCallbackQueryAsync(callbackId, "سرویس کد فعال نیست");
                await SendToChatAsync(chatId, "سرویس ارسال کد فعلاً در دسترس نیست.");
                return;
            }

            var link = await _messagingLinks.GetByChatAsync(CurrentProvider, chatId);
            if (link == null || string.IsNullOrWhiteSpace(link.PhoneNumber))
            {
                await AnswerCallbackQueryAsync(callbackId, "ابتدا شماره را وصل کنید");
                await SendToChatAsync(chatId,
                    $"هنوز شماره موبایل به این چت وصل نشده.\nابتدا شماره <b>خودتان</b> را ارسال کنید یا از اشتراک مخاطب اکانت {ProviderLabel} استفاده کنید.\nمثال: <code>09121234567</code>");
                return;
            }

            // دوباره چک تکراری بودن شماره تأییدشده
            if (_users != null)
            {
                try
                {
                    var phone = MessagingOtpService.NormalizePhone(link.PhoneNumber);
                    if (await _users.IsPhoneVerifiedByAnotherUserAsync(phone, link.UserId))
                    {
                        await AnswerCallbackQueryAsync(callbackId, "شماره تکراری است");
                        await SendToChatAsync(chatId,
                            "❌ این شماره قبلاً برای اکانت دیگری تأیید شده است. امکان دریافت کد وجود ندارد.");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Duplicate phone check failed before OTP");
                }
            }

            var result = await _otpService.SendOtpAsync(link.PhoneNumber, purpose, CurrentProvider);
            await AnswerCallbackQueryAsync(callbackId, result.Success ? "کد ارسال شد" : "ارسال ناموفق");
            if (!result.Success && !string.IsNullOrWhiteSpace(result.Message))
                await SendToChatAsync(chatId, result.Message);
        }
    }
}

