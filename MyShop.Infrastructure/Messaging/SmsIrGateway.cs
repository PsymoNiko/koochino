using Microsoft.Extensions.Configuration;
using MyShop.Application.Interfaces.Messaging;
using MyShop.Application.Interfaces.Security;
using MyShop.Application.Services.Security;
using MyShop.Core.Interfaces;
using MyShop.Core.Models.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MyShop.Infrastructure.Messaging
{
    /// <summary>
    /// sms.ir REST gateway.
    /// Bulk: POST /v1/send/bulk — Pack report: GET /v1/send/pack/{id} — Message report: GET /v1/send/{id}
    /// Verify (OTP templates): POST /v1/send/verify
    /// </summary>
    public class SmsIrGateway : ISmsSender, ISmsAdminService
    {
        private const string BulkEndpoint = "https://api.sms.ir/v1/send/bulk";
        private const string VerifyEndpoint = "https://api.sms.ir/v1/send/verify";
        private const string PackReportEndpoint = "https://api.sms.ir/v1/send/pack";
        private const string MessageReportEndpoint = "https://api.sms.ir/v1/send";
        private const string ProviderName = "SmsIr";
        private readonly HttpClient _http;
        private readonly IConfiguration _configuration;
        private readonly IMessagingSendLogRepository _logs;
        private readonly ISmsLineRepository _lines;
        private readonly IModuleConfigService _moduleConfig;
        private readonly ICurrentTenant _tenant;

        public SmsIrGateway(
            HttpClient http,
            IConfiguration configuration,
            IMessagingSendLogRepository logs,
            ISmsLineRepository lines,
            IModuleConfigService moduleConfig = null,
            ICurrentTenant tenant = null)
        {
            _http = http;
            _configuration = configuration;
            _logs = logs;
            _lines = lines;
            _moduleConfig = moduleConfig;
            _tenant = tenant;
        }

        private async Task<string> ConfigLineNumberAsync()
        {
            var fromMod = await ModuleConfigRead.TryGetAsync(_moduleConfig, ModuleConfigRead.SiteIdOf(_tenant), "Sms", "LineNumber", secret: false);
            if (!string.IsNullOrWhiteSpace(fromMod)) return fromMod;
            return _configuration["SmsIr:LineNumber"] ?? "";
        }

        private async Task<bool> UseSandboxAsync(string provider = ProviderName)
        {
            try
            {
                var settings = await _lines.GetProviderSettingsAsync(provider);
                if (settings != null)
                    return settings.UseSandbox;
            }
            catch { }
            if (!string.Equals(provider, ProviderName, StringComparison.OrdinalIgnoreCase))
                return false;
            var fromMod = await ModuleConfigRead.TryGetAsync(_moduleConfig, ModuleConfigRead.SiteIdOf(_tenant), "Sms", "UseSandbox", secret: false);
            if (bool.TryParse(fromMod, out var flag))
                return flag;
            return _configuration.GetValue<bool>("SmsIr:UseSandbox");
        }

        private static string LineProvider(SmsLine line) =>
            string.IsNullOrWhiteSpace(line?.Provider) ? SmsProviders.SmsIr : line.Provider.Trim();

        private async Task<string> GetApiKeyAsync(SmsLine line = null)
        {
            var provider = LineProvider(line);
            var useSandbox = await UseSandboxAsync(provider);
            if (line != null)
            {
                var fromLine = useSandbox ? line.SandboxApiKey : line.ApiKey;
                if (!string.IsNullOrWhiteSpace(fromLine))
                    return fromLine.Trim();
            }

            if (!string.Equals(provider, ProviderName, StringComparison.OrdinalIgnoreCase))
            {
                var meliKeyName = useSandbox ? "MeliPayamak:SandboxApiKey" : "MeliPayamak:ApiKey";
                return _configuration[meliKeyName] ?? "";
            }

            var keyName = useSandbox ? "SandboxApiKey" : "ApiKey";
            var fromMod = await ModuleConfigRead.TryGetAsync(_moduleConfig, ModuleConfigRead.SiteIdOf(_tenant), "Sms", keyName, secret: true);
            if (!string.IsNullOrWhiteSpace(fromMod))
                return fromMod;
            return _configuration[useSandbox ? "SmsIr:SandboxApiKey" : "SmsIr:ApiKey"] ?? "";
        }

        private Task<string> GetMeliUsernameAsync(SmsLine line = null)
        {
            if (!string.IsNullOrWhiteSpace(line?.Username))
                return Task.FromResult(line.Username.Trim());
            var fromCfg = _configuration["MeliPayamak:Username"];
            return Task.FromResult(string.IsNullOrWhiteSpace(fromCfg) ? "" : fromCfg.Trim());
        }

        private async Task<SmsLine> ResolveLineAsync(int? lineId)
        {
            try
            {
                if (lineId.HasValue && lineId.Value > 0)
                {
                    var line = await _lines.GetByIdAsync(lineId.Value);
                    if (line != null && line.IsActive)
                        return line;
                }
                return await _lines.GetDefaultAsync(null);
            }
            catch
            {
                return null;
            }
        }

        public async Task<SmsSendResult> SendAsync(string phone, string message, int? lineId = null, string messageType = null, string templateCode = null)
        {
            if (string.IsNullOrWhiteSpace(phone) || string.IsNullOrWhiteSpace(message))
                return new SmsSendResult(false, Error: "شماره موبایل و متن پیام الزامی است.");

            var line = await ResolveLineAsync(lineId);
            var apiKey = await GetApiKeyAsync(line);
            if (string.IsNullOrWhiteSpace(apiKey))
                return new SmsSendResult(false, Error: "کلید API پیامک تنظیم نشده است. برای این خط ApiKey / SandboxApiKey را وارد کنید.");

            var lineNumber = line?.LineNumber?.Trim() ?? (await ConfigLineNumberAsync())?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(lineNumber))
                return new SmsSendResult(false, Error: "شماره خط پیامک تنظیم نشده است.");

            var provider = LineProvider(line);
            var mobile = NormalizePhone(phone);
            SmsSendResult result;
            if (string.Equals(provider, SmsProviders.MeliPayamak, StringComparison.OrdinalIgnoreCase))
            {
                var username = await GetMeliUsernameAsync(line);
                result = await MeliPayamakClient.SendOneAsync(_http, username, apiKey, lineNumber, mobile, message);
            }
            else
            {
                try
                {
                    using var request = new HttpRequestMessage(HttpMethod.Post, BulkEndpoint);
                    ApplySmsIrHeaders(request, apiKey);
                    var payload = new Dictionary<string, object>
                    {
                        ["lineNumber"] = long.TryParse(lineNumber, out var ln) ? ln : 0,
                        ["messageText"] = message,
                        ["mobiles"] = new[] { mobile }
                    };
                    request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                    using var response = await _http.SendAsync(request);
                    var body = await response.Content.ReadAsStringAsync();
                    result = ParseSendResponse(response.IsSuccessStatusCode, body, (int)response.StatusCode);
                }
                catch (Exception ex)
                {
                    result = new SmsSendResult(false, Error: ex.Message);
                }
            }

            await LogAsync(mobile, message, result, lineNumber, messageType ?? MessagingMessageTypes.Text, templateCode, await UseSandboxAsync(provider), provider);
            return result;
        }

        public Task<SmsSendResult> SendTestAsync(string phone, int? lineId = null)
            => SendAsync(phone, "پیام آزمایشی سامانه با موفقیت ارسال شد.", lineId, MessagingMessageTypes.Test);

        public async Task<SmsSendResult> SendVerifyAsync(
            string phone,
            int templateId,
            IDictionary<string, string> parameters,
            string messageType = null,
            string templateCode = null)
        {
            if (string.IsNullOrWhiteSpace(phone) || templateId <= 0)
                return new SmsSendResult(false, Error: "شماره و شناسه قالب sms.ir الزامی است.");

            var line = await ResolveLineAsync(null);
            var provider = LineProvider(line);
            if (!string.Equals(provider, ProviderName, StringComparison.OrdinalIgnoreCase))
                return new SmsSendResult(false, Error: "ارسال با قالب تاییدیه (Verify) فقط برای خطوط sms.ir پشتیبانی می‌شود.");

            var apiKey = await GetApiKeyAsync(line);
            if (string.IsNullOrWhiteSpace(apiKey))
                return new SmsSendResult(false, Error: "کلید API پیامک تنظیم نشده است. برای خط پیش‌فرض ApiKey / SandboxApiKey را وارد کنید.");

            var mobile = NormalizePhone(phone);
            var paramList = (parameters ?? new Dictionary<string, string>())
                .Where(x => !string.IsNullOrWhiteSpace(x.Key))
                .Select(x => new { name = x.Key.Trim(), value = x.Value ?? "" })
                .ToArray();

            var preview = string.Join(", ", paramList.Select(p => $"{p.name}={p.value}"));
            SmsSendResult result;
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, VerifyEndpoint);
                ApplySmsIrHeaders(request, apiKey);
                var payload = new { mobile, templateId, parameters = paramList };
                request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                using var response = await _http.SendAsync(request);
                var body = await response.Content.ReadAsStringAsync();
                result = ParseSendResponse(response.IsSuccessStatusCode, body, (int)response.StatusCode);
            }
            catch (Exception ex)
            {
                result = new SmsSendResult(false, Error: ex.Message);
            }

            await LogAsync(mobile, $"[verify:{templateId}] {preview}", result, line?.LineNumber,
                messageType ?? MessagingMessageTypes.Otp, templateCode, await UseSandboxAsync(provider), provider);
            return result;
        }

        public async Task<SmsSendResult> SendMeliSharedAsync(string phone, int bodyId, string[] args, int? lineId = null, string messageType = null, string templateCode = null)
        {
            if (string.IsNullOrWhiteSpace(phone) || bodyId <= 0)
                return new SmsSendResult(false, Error: "شماره یا BodyId نامعتبر است.");

            var line = await ResolveLineAsync(lineId);
            var provider = LineProvider(line);
            if (!string.Equals(provider, SmsProviders.MeliPayamak, StringComparison.OrdinalIgnoreCase))
                return new SmsSendResult(false, Error: "ارسال با الگوی پیامکی مشترک فقط برای خطوط ملی‌پیامک پشتیبانی می‌شود.");

            var apiKey = await GetApiKeyAsync(line);
            if (string.IsNullOrWhiteSpace(apiKey))
                return new SmsSendResult(false, Error: "کلید API پنل ملی‌پیامک تنظیم نشده است.");

            var username = await GetMeliUsernameAsync(line);
            if (string.IsNullOrWhiteSpace(username))
                return new SmsSendResult(false, Error: "نام کاربری پنل ملی‌پیامک برای خط تنظیم نشده است (SendByBaseNumber2).");

            var mobile = NormalizePhone(phone);
            var result = await MeliPayamakClient.SendByBaseNumberAsync(_http, username, apiKey, mobile, bodyId, args);

            var preview = $"[shared:{bodyId}] " + string.Join(", ", args ?? Array.Empty<string>());
            await LogAsync(mobile, preview, result, line?.LineNumber,
                messageType ?? MessagingMessageTypes.Otp, templateCode, await UseSandboxAsync(provider), provider);
            return result;
        }

        public async Task<SmsSendResult> SendBulkAsync(SmsBulkSendRequest request)
        {
            if (request == null || request.Phones == null || request.Phones.Count == 0)
                return new SmsSendResult(false, Error: "حداقل یک شماره الزامی است.");
            if (string.IsNullOrWhiteSpace(request.MessageText))
                return new SmsSendResult(false, Error: "متن پیام الزامی است.");

            var line = await ResolveLineAsync(request.LineId);
            var apiKey = await GetApiKeyAsync(line);
            if (string.IsNullOrWhiteSpace(apiKey))
                return new SmsSendResult(false, Error: "کلید API پیامک تنظیم نشده است. برای این خط ApiKey / SandboxApiKey را وارد کنید.");

            var lineNumber = line?.LineNumber?.Trim() ?? (await ConfigLineNumberAsync())?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(lineNumber))
                return new SmsSendResult(false, Error: "شماره خط پیامک تنظیم نشده است.");

            var normalizedPhones = request.Phones
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Select(NormalizePhone)
                .Where(p => p.Length >= 10)
                .Distinct()
                .ToArray();

            if (normalizedPhones.Length == 0)
                return new SmsSendResult(false, Error: "هیچ شماره معتبری وارد نشده.");

            var provider = LineProvider(line);
            SmsSendResult result;
            if (string.Equals(provider, SmsProviders.MeliPayamak, StringComparison.OrdinalIgnoreCase))
            {
                var username = await GetMeliUsernameAsync(line);
                result = await MeliPayamakClient.SendBulkAsync(_http, username, apiKey, lineNumber, normalizedPhones, request.MessageText);
            }
            else
            {
                try
                {
                    using var httpReq = new HttpRequestMessage(HttpMethod.Post, BulkEndpoint);
                    ApplySmsIrHeaders(httpReq, apiKey);
                    var payload = new Dictionary<string, object>
                    {
                        ["lineNumber"] = long.TryParse(lineNumber, out var ln) ? ln : 0,
                        ["messageText"] = request.MessageText,
                        ["mobiles"] = normalizedPhones
                    };
                    httpReq.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                    using var response = await _http.SendAsync(httpReq);
                    var body = await response.Content.ReadAsStringAsync();
                    result = ParseSendResponse(response.IsSuccessStatusCode, body, (int)response.StatusCode);
                }
                catch (Exception ex)
                {
                    result = new SmsSendResult(false, Error: ex.Message);
                }
            }

            var msgType = string.IsNullOrWhiteSpace(request.MessageType) ? MessagingMessageTypes.Manual : request.MessageType;
            var sandbox = await UseSandboxAsync(provider);
            foreach (var phone in normalizedPhones)
                await LogAsync(phone, request.MessageText, result, lineNumber, msgType, request.TemplateCode, sandbox, provider);
            return result;
        }

        public async Task<List<SmsDeliveryReport>> GetPackReportAsync(string packId)
        {
            packId = NormalizeExternalId(packId);
            if (string.IsNullOrWhiteSpace(packId))
                return new List<SmsDeliveryReport>();

            foreach (var apiKey in await GetApiKeysToTryAsync(null))
            {
                try
                {
                    using var req = new HttpRequestMessage(HttpMethod.Get, $"{PackReportEndpoint}/{packId}");
                    ApplySmsIrHeaders(req, apiKey);
                    using var response = await _http.SendAsync(req);
                    var body = await response.Content.ReadAsStringAsync();
                    if (!response.IsSuccessStatusCode) continue;
                    var list = ParseDeliveryArray(body);
                    if (list.Count > 0) return list;
                    if (IsSmsIrSuccess(body)) return list;
                }
                catch { }
            }
            return new List<SmsDeliveryReport>();
        }

        public async Task<SmsDeliveryReport> GetMessageReportAsync(string messageId)
        {
            messageId = NormalizeExternalId(messageId);
            if (string.IsNullOrWhiteSpace(messageId) || !long.TryParse(messageId, out _))
                return null;

            foreach (var apiKey in await GetApiKeysToTryAsync(null))
            {
                try
                {
                    using var req = new HttpRequestMessage(HttpMethod.Get, $"{MessageReportEndpoint}/{messageId}");
                    ApplySmsIrHeaders(req, apiKey);
                    using var response = await _http.SendAsync(req);
                    var body = await response.Content.ReadAsStringAsync();
                    if (!response.IsSuccessStatusCode) continue;

                    using var doc = JsonDocument.Parse(body);
                    if (!TryGetProp(doc.RootElement, "data", out var data))
                        continue;
                    if (data.ValueKind == JsonValueKind.Object)
                        return ParseDeliveryItem(data);
                    if (data.ValueKind == JsonValueKind.Array && data.GetArrayLength() > 0)
                        return ParseDeliveryItem(data[0]);
                }
                catch { }
            }
            return null;
        }

        public async Task<string> RefreshDeliveryForLogAsync(MessagingSendLog log)
        {
            if (log == null)
                return "رکورد نامعتبر است.";

            var rawId = NormalizeExternalId(log.ExternalId);
            if (string.IsNullOrWhiteSpace(rawId))
                return "شناسه ارسال (packId/messageId) در لاگ ذخیره نشده. پیامک ممکن است Sandbox بوده یا پاسخ sms.ir ناقص بوده است.";

            SmsDeliveryReport report = null;
            var tried = new List<string>();

            if (Guid.TryParse(rawId, out _) || rawId.Length >= 32)
            {
                tried.Add("pack");
                var pack = await GetPackReportAsync(rawId);
                report = pack.FirstOrDefault(x =>
                             NormalizePhone(x.Mobile ?? "") == NormalizePhone(log.PhoneNumber ?? ""))
                         ?? pack.FirstOrDefault();
            }

            if (report == null && long.TryParse(rawId, out _))
            {
                tried.Add("message");
                report = await GetMessageReportAsync(rawId);
            }

            if (report == null && !Guid.TryParse(rawId, out _))
            {
                tried.Add("pack-fallback");
                var pack = await GetPackReportAsync(rawId);
                report = pack.FirstOrDefault();
            }

            if (report == null)
                return "sms.ir گزارشی برنگرداند. اگر Sandbox روشن باشد، در پنل واقعی چیزی ثبت نمی‌شود. شناسه: " + rawId;

            var text = report.DeliveryStateText;
            await _logs.UpdateDeliveryAsync(log.Id, report.DeliveryState, text);
            log.DeliveryState = report.DeliveryState;
            log.DeliveryStateText = text;
            log.DeliveryCheckedAt = DateTime.Now;
            return text;
        }

        public SmsConfigurationStatus GetConfigurationStatus()
            => GetConfigurationStatusAsync(null).GetAwaiter().GetResult();

        public async Task<SmsConfigurationStatus> GetConfigurationStatusAsync(int? siteId = null)
        {
            var useSandbox = await UseSandboxAsync();
            List<SmsLine> lineList;
            try { lineList = await _lines.ListAsync(siteId); }
            catch { lineList = new List<SmsLine>(); }

            var defaultLine = lineList.FirstOrDefault(x => x.IsDefault && x.IsActive)
                ?? lineList.FirstOrDefault(x => x.IsActive);
            var lineNumber = defaultLine?.LineNumber ?? await ConfigLineNumberAsync();
            var key = await GetApiKeyAsync(defaultLine);
            var masked = SmsLine.MaskSecret(key);

            return new SmsConfigurationStatus(
                !string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(lineNumber),
                useSandbox,
                useSandbox ? "Sandbox" : "Production",
                masked,
                lineNumber ?? "",
                lineList,
                defaultLine?.Id);
        }

        public async Task<List<MessagingSendLog>> GetRecentLogsAsync(int top = 50)
        {
            var smsIr = await _logs.ListAsync(SmsProviders.SmsIr, top);
            var meli = await _logs.ListAsync(SmsProviders.MeliPayamak, top);
            return smsIr.Concat(meli)
                .OrderByDescending(x => x.CreatedAt)
                .Take(top)
                .ToList();
        }

        public Task<List<MessagingSendLog>> SearchLogsAsync(SmsSendLogFilter filter) => _logs.SearchAsync(filter);
        public Task<List<SmsLine>> ListLinesAsync(int? siteId = null) => _lines.ListAsync(siteId);
        public Task<int> SaveLineAsync(SmsLine line, bool setAsDefault = false) => _lines.UpsertAsync(line, setAsDefault);
        public Task DeleteLineAsync(int id) => _lines.DeleteAsync(id);
        public Task SetDefaultLineAsync(int id, int? siteId = null) => _lines.SetDefaultAsync(id, siteId);
        public Task SetUseSandboxAsync(bool useSandbox) => _lines.SetUseSandboxAsync(useSandbox, ProviderName);

        private async Task<string> ResolveLineNumberAsync(int? lineId)
        {
            var line = await ResolveLineAsync(lineId);
            if (!string.IsNullOrWhiteSpace(line?.LineNumber))
                return line.LineNumber.Trim();
            return (await ConfigLineNumberAsync())?.Trim() ?? "";
        }

        private async Task LogAsync(string phone, string message, SmsSendResult result, string lineNumber, string messageType, string templateCode, bool sandbox = false, string provider = ProviderName)
        {
            try
            {
                var error = result.Error;
                if (sandbox && result.Ok)
                    error = string.IsNullOrWhiteSpace(error)
                        ? $"Sandbox: پیامک واقعی به گوشی/پنل {SmsProviders.DisplayName(provider)} ارسال نمی‌شود."
                        : error;
                await _logs.InsertAsync(new MessagingSendLog
                {
                    Provider = provider,
                    PhoneNumber = phone,
                    MessageType = messageType ?? MessagingMessageTypes.Text,
                    MessageText = message,
                    Success = result.Ok,
                    ErrorMessage = error,
                    LineNumber = lineNumber,
                    ExternalId = result.MessageId,
                    TemplateCode = templateCode,
                    DeliveryStateText = sandbox && result.Ok ? "Sandbox (ارسال نشده)" : null
                });
            }
            catch { }
        }

        private static void ApplySmsIrHeaders(HttpRequestMessage request, string apiKey)
        {
            request.Headers.TryAddWithoutValidation("x-api-key", apiKey);
            request.Headers.TryAddWithoutValidation("Accept", "application/json");
        }

        private async Task<List<string>> GetApiKeysToTryAsync(SmsLine line)
        {
            line ??= await ResolveLineAsync(null);
            var keys = new List<string>();
            void Add(string k)
            {
                k = k?.Trim();
                if (!string.IsNullOrWhiteSpace(k) && !keys.Contains(k, StringComparer.Ordinal))
                    keys.Add(k);
            }
            var current = await GetApiKeyAsync(line);
            Add(current);
            if (line != null)
            {
                Add(line.ApiKey);
                Add(line.SandboxApiKey);
            }
            Add(_configuration["SmsIr:ApiKey"]);
            Add(_configuration["SmsIr:SandboxApiKey"]);
            Add(await ModuleConfigRead.TryGetAsync(_moduleConfig, ModuleConfigRead.SiteIdOf(_tenant), "Sms", "ApiKey", true));
            Add(await ModuleConfigRead.TryGetAsync(_moduleConfig, ModuleConfigRead.SiteIdOf(_tenant), "Sms", "SandboxApiKey", true));
            return keys;
        }

        private static SmsSendResult ParseSendResponse(bool httpOk, string body, int statusCode)
        {
            try
            {
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;
                var status = ReadInt(root, "status");
                var message = ReadString(root, "message") ?? "";
                TryGetProp(root, "data", out var data);

                if (status != 1)
                {
                    var detail = string.IsNullOrWhiteSpace(message) ? body : message;
                    return new SmsSendResult(false, Error: $"sms.ir status={status}: {detail}");
                }

                var externalId = ExtractExternalId(data);
                if (data.ValueKind == JsonValueKind.Object && TryGetProp(data, "messageIds", out var ids)
                    && ids.ValueKind == JsonValueKind.Array && ids.GetArrayLength() > 0)
                {
                    var first = ids[0];
                    if (first.ValueKind == JsonValueKind.Null)
                        return new SmsSendResult(false, Error: "شماره نامعتبر است یا متن بیش از حد مجاز است.");
                    if (first.ValueKind == JsonValueKind.Number && first.GetInt64() == 0)
                        return new SmsSendResult(false, Error: "شماره در لیست سیاه sms.ir است.");
                    if (string.IsNullOrWhiteSpace(externalId) && first.ValueKind == JsonValueKind.Number)
                        externalId = first.GetInt64().ToString();
                }

                return new SmsSendResult(true, externalId);
            }
            catch
            {
                if (httpOk)
                    return new SmsSendResult(true, null);
                return new SmsSendResult(false, Error: $"sms.ir HTTP {statusCode}: {body}");
            }
        }

        private static string ExtractExternalId(JsonElement data)
        {
            if (data.ValueKind == JsonValueKind.Undefined || data.ValueKind == JsonValueKind.Null)
                return null;
            if (data.ValueKind == JsonValueKind.String)
                return data.GetString()?.Trim().Trim('"');
            if (data.ValueKind == JsonValueKind.Number)
                return data.ToString();
            if (data.ValueKind == JsonValueKind.Object)
            {
                var pack = ReadString(data, "packId");
                if (!string.IsNullOrWhiteSpace(pack)) return pack.Trim().Trim('"');
                var mid = ReadString(data, "messageId");
                if (!string.IsNullOrWhiteSpace(mid)) return mid;
            }
            var raw = data.ToString()?.Trim().Trim('"');
            return NormalizeExternalId(raw);
        }

        private static string NormalizeExternalId(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "";
            var v = value.Trim().Trim('"');
            if (v.StartsWith("{"))
            {
                try
                {
                    using var doc = JsonDocument.Parse(v);
                    var pack = ReadString(doc.RootElement, "packId");
                    if (!string.IsNullOrWhiteSpace(pack)) return pack.Trim().Trim('"');
                    var mid = ReadString(doc.RootElement, "messageId");
                    if (!string.IsNullOrWhiteSpace(mid)) return mid;
                }
                catch { }
            }
            return v;
        }

        private static bool IsSmsIrSuccess(string body)
        {
            try
            {
                using var doc = JsonDocument.Parse(body);
                return ReadInt(doc.RootElement, "status") == 1;
            }
            catch { return false; }
        }

        private static List<SmsDeliveryReport> ParseDeliveryArray(string body)
        {
            var list = new List<SmsDeliveryReport>();
            using var doc = JsonDocument.Parse(body);
            if (!TryGetProp(doc.RootElement, "data", out var data))
                return list;
            if (data.ValueKind == JsonValueKind.Object)
            {
                list.Add(ParseDeliveryItem(data));
                return list;
            }
            if (data.ValueKind != JsonValueKind.Array)
                return list;
            foreach (var item in data.EnumerateArray())
                list.Add(ParseDeliveryItem(item));
            return list;
        }

        private static SmsDeliveryReport ParseDeliveryItem(JsonElement item)
        {
            return new SmsDeliveryReport
            {
                MessageId = ReadInt(item, "messageId"),
                Mobile = ReadString(item, "mobile") ?? "",
                MessageText = ReadString(item, "messageText") ?? "",
                SendDateTime = ReadLong(item, "sendDateTime"),
                LineNumber = ReadLong(item, "lineNumber") ?? 0,
                Cost = ReadDecimal(item, "cost"),
                DeliveryState = ReadByte(item, "deliveryState"),
                DeliveryDateTime = ReadLong(item, "deliveryDateTime")
            };
        }

        private static bool TryGetProp(JsonElement el, string name, out JsonElement value)
        {
            if (el.ValueKind == JsonValueKind.Object)
            {
                foreach (var p in el.EnumerateObject())
                {
                    if (string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase))
                    {
                        value = p.Value;
                        return true;
                    }
                }
            }
            value = default;
            return false;
        }

        private static string ReadString(JsonElement el, string name)
        {
            if (!TryGetProp(el, name, out var v)) return null;
            return v.ValueKind switch
            {
                JsonValueKind.String => v.GetString(),
                JsonValueKind.Number => v.ToString(),
                JsonValueKind.Null => null,
                _ => v.ToString()
            };
        }

        private static int ReadInt(JsonElement el, string name)
        {
            if (!TryGetProp(el, name, out var v) || v.ValueKind != JsonValueKind.Number) return 0;
            return v.TryGetInt32(out var i) ? i : (int)v.GetInt64();
        }

        private static long? ReadLong(JsonElement el, string name)
        {
            if (!TryGetProp(el, name, out var v) || v.ValueKind != JsonValueKind.Number) return null;
            return v.TryGetInt64(out var i) ? i : null;
        }

        private static byte? ReadByte(JsonElement el, string name)
        {
            if (!TryGetProp(el, name, out var v) || v.ValueKind != JsonValueKind.Number) return null;
            var n = v.TryGetInt32(out var i) ? i : 0;
            if (n < 0 || n > 255) return null;
            return (byte)n;
        }

        private static decimal ReadDecimal(JsonElement el, string name)
        {
            if (!TryGetProp(el, name, out var v) || v.ValueKind != JsonValueKind.Number) return 0;
            return v.TryGetDecimal(out var d) ? d : 0;
        }

        public static string NormalizePhone(string phone)
        {
            var value = (phone ?? "").Trim().Replace(" ", "").Replace("-", "");
            var sb = new StringBuilder(value.Length);
            foreach (var c in value)
            {
                if (c >= '۰' && c <= '۹') sb.Append((char)('0' + (c - '۰')));
                else if (c >= '٠' && c <= '٩') sb.Append((char)('0' + (c - '٠')));
                else sb.Append(c);
            }
            value = sb.ToString();
            if (value.StartsWith("+98")) value = "0" + value.Substring(3);
            else if (value.StartsWith("98") && value.Length >= 12) value = "0" + value.Substring(2);
            value = new string(value.Where(char.IsDigit).ToArray());
            if (value.StartsWith("9") && value.Length == 10) value = "0" + value;
            return value;
        }
    }
}
