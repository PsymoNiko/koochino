using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using MyShop.Application.Interfaces.Messaging;

namespace MyShop.Infrastructure.Messaging
{
    /// <summary>
    /// ملی‌پیامک — وب‌سرویس کلاسیک پنل (خط خدماتی / پترن).
    /// Pattern: POST https://rest.payamak-panel.com/api/SendSMS/BaseServiceNumber  (= SendByBaseNumber2)
    /// Simple:  POST https://rest.payamak-panel.com/api/SendSMS/SendSMS
    /// Docs: https://github.com/Melipayamak/melipayamak-Csharp
    /// </summary>
    public static class MeliPayamakClient
    {
        private const string RestEndpoint = "https://rest.payamak-panel.com/api/SendSMS/";
        private const string BaseServiceNumberOp = "BaseServiceNumber";
        private const string SendSmsOp = "SendSMS";

        /// <summary>
        /// ارسال با پترن تأییدشده (خط خدماتی) — معادل SOAP SendByBaseNumber2.
        /// <paramref name="args"/> به ترتیب جای خالی‌های پترن با جداکننده ; در پارامتر text فرستاده می‌شود.
        /// </summary>
        public static async Task<SmsSendResult> SendByBaseNumberAsync(
            HttpClient http,
            string username,
            string password,
            string to,
            int bodyId,
            string[] args)
        {
            if (string.IsNullOrWhiteSpace(username))
                return new SmsSendResult(false, Error: "نام کاربری پنل ملی‌پیامک تنظیم نشده است.");
            if (string.IsNullOrWhiteSpace(password))
                return new SmsSendResult(false, Error: "رمز/کلید API پنل ملی‌پیامک تنظیم نشده است.");
            if (bodyId <= 0)
                return new SmsSendResult(false, Error: "شناسه پترن (BodyId / کد متن) ملی‌پیامک تنظیم نشده است.");
            if (string.IsNullOrWhiteSpace(to))
                return new SmsSendResult(false, Error: "شماره گیرنده نامعتبر است.");

            var text = string.Join(";", (args ?? Array.Empty<string>()).Select(a => a ?? ""));
            var form = new Dictionary<string, string>
            {
                ["username"] = username.Trim(),
                ["password"] = password.Trim(),
                ["text"] = text,
                ["to"] = to.Trim(),
                ["bodyId"] = bodyId.ToString()
            };

            return await PostFormAsync(http, BaseServiceNumberOp, form);
        }

        /// <summary>سازگاری با نام قبلی — همان SendByBaseNumber.</summary>
        public static Task<SmsSendResult> SendSharedAsync(
            HttpClient http,
            string username,
            string password,
            int bodyId,
            string to,
            string[] args)
            => SendByBaseNumberAsync(http, username, password, to, bodyId, args);

        public static async Task<SmsSendResult> SendOneAsync(
            HttpClient http,
            string username,
            string password,
            string from,
            string to,
            string text)
        {
            if (string.IsNullOrWhiteSpace(username))
                return new SmsSendResult(false, Error: "نام کاربری پنل ملی‌پیامک تنظیم نشده است.");
            if (string.IsNullOrWhiteSpace(password))
                return new SmsSendResult(false, Error: "رمز/کلید API پنل ملی‌پیامک تنظیم نشده است.");

            var form = new Dictionary<string, string>
            {
                ["username"] = username.Trim(),
                ["password"] = password.Trim(),
                ["to"] = to ?? "",
                ["from"] = from ?? "",
                ["text"] = text ?? "",
                ["isFlash"] = "false"
            };

            return await PostFormAsync(http, SendSmsOp, form);
        }

        public static async Task<SmsSendResult> SendSharedBulkAsync(
            HttpClient http,
            string username,
            string password,
            int bodyId,
            string[] phones,
            string text)
        {
            if (phones == null || phones.Length == 0)
                return new SmsSendResult(false, Error: "حداقل یک شماره الزامی است.");

            var ids = new List<string>();
            var errors = new List<string>();

            foreach (var phone in phones)
            {
                var result = await SendByBaseNumberAsync(http, username, password, phone, bodyId, new[] { text });
                if (result.Ok)
                {
                    if (!string.IsNullOrWhiteSpace(result.MessageId))
                        ids.Add(result.MessageId);
                }
                else
                {
                    errors.Add($"{phone}: {result.Error}");
                }
            }

            if (errors.Count == 0)
                return new SmsSendResult(true, string.Join(",", ids));

            if (ids.Count > 0)
                return new SmsSendResult(true, string.Join(",", ids), Error: "برخی شماره‌ها ارسال نشدند: " + string.Join(" | ", errors));

            return new SmsSendResult(false, Error: string.Join(" | ", errors));
        }

        public static async Task<SmsSendResult> SendBulkAsync(
            HttpClient http,
            string username,
            string password,
            string from,
            string[] phones,
            string text)
        {
            if (phones == null || phones.Length == 0)
                return new SmsSendResult(false, Error: "حداقل یک شماره الزامی است.");

            var ids = new List<string>();
            var errors = new List<string>();

            foreach (var phone in phones)
            {
                var result = await SendOneAsync(http, username, password, from, phone, text);
                if (result.Ok)
                {
                    if (!string.IsNullOrWhiteSpace(result.MessageId))
                        ids.Add(result.MessageId);
                }
                else
                {
                    errors.Add($"{phone}: {result.Error}");
                }
            }

            if (errors.Count == 0)
                return new SmsSendResult(true, string.Join(",", ids));

            if (ids.Count > 0)
                return new SmsSendResult(true, string.Join(",", ids), Error: "برخی شماره‌ها ارسال نشدند: " + string.Join(" | ", errors));

            return new SmsSendResult(false, Error: string.Join(" | ", errors));
        }

        private static async Task<SmsSendResult> PostFormAsync(HttpClient http, string op, Dictionary<string, string> form)
        {
            try
            {
                using var content = new FormUrlEncodedContent(form);
                using var response = await http.PostAsync(RestEndpoint + op, content);
                var body = await response.Content.ReadAsStringAsync();
                return ParseRestResponse(response.IsSuccessStatusCode, body, (int)response.StatusCode);
            }
            catch (Exception ex)
            {
                return new SmsSendResult(false, Error: ex.Message);
            }
        }

        private static SmsSendResult ParseRestResponse(bool httpOk, string body, int statusCode)
        {
            var raw = (body ?? "").Trim();
            if (string.IsNullOrWhiteSpace(raw))
                return httpOk
                    ? new SmsSendResult(true, null)
                    : new SmsSendResult(false, Error: $"ملی‌پیامک HTTP {statusCode}: پاسخ خالی");

            try
            {
                using var doc = JsonDocument.Parse(raw);
                var root = doc.RootElement;

                string value = null;
                int? retStatus = null;
                string strRet = null;

                if (root.ValueKind == JsonValueKind.Object)
                {
                    if (root.TryGetProperty("Value", out var v) || root.TryGetProperty("value", out v))
                        value = v.ToString();
                    if (root.TryGetProperty("RetStatus", out var rs) || root.TryGetProperty("retStatus", out rs))
                    {
                        if (rs.ValueKind == JsonValueKind.Number && rs.TryGetInt32(out var n))
                            retStatus = n;
                        else if (int.TryParse(rs.ToString(), out var n2))
                            retStatus = n2;
                    }
                    if (root.TryGetProperty("StrRetStatus", out var s) || root.TryGetProperty("strRetStatus", out s))
                        strRet = s.ToString();
                }
                else if (root.ValueKind == JsonValueKind.String || root.ValueKind == JsonValueKind.Number)
                {
                    value = root.ToString();
                }

                // موفقیت: Value عددی مثبت (recId) و معمولاً RetStatus=1
                if (long.TryParse(value, out var recId) && recId > 0)
                    return new SmsSendResult(true, recId.ToString());

                var err = !string.IsNullOrWhiteSpace(strRet)
                    ? strRet
                    : (retStatus.HasValue ? $"کد {retStatus}" : raw);

                if (retStatus == 1 && !string.IsNullOrWhiteSpace(value) && value != "0")
                    return new SmsSendResult(true, value);

                return new SmsSendResult(false, Error: $"ملی‌پیامک: {err}");
            }
            catch
            {
                // SOAP/XML string body fallback: <string>recId</string>
                if (long.TryParse(raw.Trim().Trim('<', '>'), out var id) && id > 0)
                    return new SmsSendResult(true, id.ToString());

                var dig = System.Text.RegularExpressions.Regex.Match(raw, @">(-?\d+)<");
                if (dig.Success && long.TryParse(dig.Groups[1].Value, out var xmlId))
                {
                    if (xmlId > 0)
                        return new SmsSendResult(true, xmlId.ToString());
                    return new SmsSendResult(false, Error: $"ملی‌پیامک خطا (کد {xmlId})");
                }

                return httpOk
                    ? new SmsSendResult(false, Error: raw)
                    : new SmsSendResult(false, Error: $"ملی‌پیامک HTTP {statusCode}: {raw}");
            }
        }
    }
}
