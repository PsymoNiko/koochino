using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using MyShop.Application.Interfaces;
using MyShop.Application.Interfaces.Messaging;
using MyShop.Core.Models.Messaging;

namespace MyShop.Web.Services
{
    public class PasswordResetService : IPasswordResetService
    {
        private const int CodeTtlMinutes = 5;
        private const int MaxAttempts = 5;
        private const int MaxSendPerWindow = 5;
        private static readonly TimeSpan SendWindow = TimeSpan.FromMinutes(10);
        private const string TemplateCode = "PasswordReset";

        private readonly IAuthService _auth;
        private readonly ISmsSender _sms;
        private readonly ISmsTemplateService _templates;
        private readonly IMemoryCache _cache;

        public PasswordResetService(
            IAuthService auth,
            ISmsSender sms,
            IMemoryCache cache,
            ISmsTemplateService templates = null)
        {
            _auth = auth;
            _sms = sms;
            _cache = cache;
            _templates = templates;
        }

        public async Task<(bool ok, string message)> SendResetCodeAsync(string phone, int roleId)
        {
            var normalized = NumberFormatHelper.ToIranMobile(phone ?? "");
            if (string.IsNullOrWhiteSpace(normalized) || normalized.Length < 10)
                return (false, "شماره موبایل معتبر نیست.");

            // رمز روی حساب کاربر است؛ نقش فقط برای برگشت به صفحه ورود استفاده می‌شود
            var user = await _auth.FindByPhoneAsync(normalized);
            if (user == null && roleId is 2 or 3)
                user = await _auth.GetUserByPhoneAsync(normalized, roleId);
            if (user == null || !user.IsActive)
                return (false, "کاربری با این شماره یافت نشد.");

            var rateKey = "PwdResetRate:" + normalized;
            var sendCount = _cache.GetOrCreate(rateKey, e =>
            {
                e.AbsoluteExpirationRelativeToNow = SendWindow;
                return 0;
            });
            if (sendCount >= MaxSendPerWindow)
                return (false, "تعداد درخواست کد زیاد است. چند دقیقه دیگر تلاش کنید.");

            var code = GenerateCode(6);
            var entry = new ResetOtpEntry
            {
                CodeHash = HashCode(code),
                RoleId = roleId,
                Attempts = 0,
                ExpiresAt = DateTime.UtcNow.AddMinutes(CodeTtlMinutes)
            };
            _cache.Set(OtpKey(normalized, roleId), entry, TimeSpan.FromMinutes(CodeTtlMinutes));
            _cache.Set(rateKey, sendCount + 1, SendWindow);

            SmsSendResult sms;
            try
            {
                sms = await SendWithTemplateAsync(normalized, code);
            }
            catch (Exception ex)
            {
                _cache.Remove(OtpKey(normalized, roleId));
                return (false, "خطا در ارسال پیامک: " + ex.Message);
            }

            if (!sms.Ok)
            {
                _cache.Remove(OtpKey(normalized, roleId));
                return (false, string.IsNullOrWhiteSpace(sms.Error)
                    ? "ارسال پیامک ناموفق بود. لطفاً دوباره تلاش کنید."
                    : sms.Error);
            }

            return (true, $"کد تعویض رمز عبور به شماره شما پیامک شد. اعتبار کد {CodeTtlMinutes} دقیقه است.");
        }

        private async Task<SmsSendResult> SendWithTemplateAsync(string phone, string code)
        {
            SmsTemplate template = null;
            if (_templates != null)
            {
                try { template = await _templates.GetByCodeAsync(TemplateCode); }
                catch { template = null; }
            }

            if (template != null && template.IsActive && template.UsesMeliSharedApi)
            {
                return await _sms.SendMeliSharedAsync(
                    phone,
                    template.MeliPayamakBodyId!.Value,
                    new[] { code },
                    null,
                    MessagingMessageTypes.PasswordReset,
                    TemplateCode);
            }

            if (template != null && template.IsActive && template.UsesVerifyApi)
            {
                return await _sms.SendVerifyAsync(
                    phone,
                    template.SmsIrTemplateId!.Value,
                    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["CODE"] = code,
                        ["Code"] = code
                    },
                    MessagingMessageTypes.PasswordReset,
                    TemplateCode);
            }

            string text;
            if (template != null && template.IsActive && !string.IsNullOrWhiteSpace(template.BodyTemplate))
            {
                text = _templates.Render(template.BodyTemplate, new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["CODE"] = code,
                    ["MINUTES"] = CodeTtlMinutes.ToString()
                });
            }
            else
            {
                text = $"کد تعویض رمز عبور کوچینو: {code}\nاین کد تا {CodeTtlMinutes} دقیقه معتبر است.";
            }

            return await _sms.SendAsync(phone, text, null, MessagingMessageTypes.PasswordReset, TemplateCode);
        }

        public async Task<(bool ok, string message)> ResetPasswordAsync(string phone, int roleId, string code, string newPassword)
        {
            var normalized = NumberFormatHelper.ToIranMobile(phone ?? "");
            var cleanCode = (code ?? "").Trim();

            if (string.IsNullOrWhiteSpace(normalized) || string.IsNullOrWhiteSpace(cleanCode))
                return (false, "شماره یا کد وارد نشده است.");

            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 8)
                return (false, "رمز عبور باید حداقل ۸ کاراکتر باشد.");

            var key = OtpKey(normalized, roleId);
            if (!_cache.TryGetValue(key, out ResetOtpEntry entry) || entry == null)
                return (false, "کد معتبر یافت نشد یا منقضی شده است. دوباره درخواست کد دهید.");

            if (DateTime.UtcNow > entry.ExpiresAt)
            {
                _cache.Remove(key);
                return (false, "اعتبار کد به پایان رسیده است (۵ دقیقه). دوباره درخواست کد دهید.");
            }

            if (entry.Attempts >= MaxAttempts)
            {
                _cache.Remove(key);
                return (false, "تعداد تلاش بیش از حد است. کد جدید بگیرید.");
            }

            entry.Attempts++;
            _cache.Set(key, entry, entry.ExpiresAt - DateTime.UtcNow);

            if (!FixedTimeHexEquals(entry.CodeHash, HashCode(cleanCode)))
                return (false, "کد وارد شده نادرست است.");

            var (ok, error) = await _auth.ResetPasswordByPhoneAsync(normalized, roleId, newPassword);
            if (!ok)
                return (false, error ?? "خطا در تغییر رمز عبور.");

            _cache.Remove(key);
            return (true, "رمز عبور با موفقیت تغییر کرد. اکنون می‌توانید وارد شوید.");
        }

        private static string OtpKey(string phone, int roleId) => $"PwdReset:{phone}:{roleId}";

        private static string GenerateCode(int length)
        {
            var bytes = new byte[length];
            RandomNumberGenerator.Fill(bytes);
            var chars = new char[length];
            for (int i = 0; i < length; i++)
                chars[i] = (char)('0' + (bytes[i] % 10));
            return new string(chars);
        }

        private static string HashCode(string code)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes("koochino-pwd-reset:" + code.Trim()));
            return Convert.ToHexString(bytes);
        }

        private static bool FixedTimeHexEquals(string a, string b)
        {
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b))
                return false;
            try
            {
                var left = Convert.FromHexString(a);
                var right = Convert.FromHexString(b);
                return left.Length == right.Length &&
                       CryptographicOperations.FixedTimeEquals(left, right);
            }
            catch
            {
                return false;
            }
        }

        private sealed class ResetOtpEntry
        {
            public string CodeHash { get; set; }
            public int RoleId { get; set; }
            public int Attempts { get; set; }
            public DateTime ExpiresAt { get; set; }
        }
    }
}
