using MyShop.Application.Interfaces.Messaging;
using MyShop.Core.Models.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace MyShop.Application.Services.Messaging
{
    public class MessagingOtpService : IMessagingOtpService
    {
        private readonly IUserMessagingLinkRepository _links;
        private readonly IMessagingOtpRepository _otps;
        private readonly IMessagingSendLogRepository _sendLogs;
        private readonly IEnumerable<IMessagingChannel> _channels;
        private readonly ISmsSender _sms;
        private readonly ISmsTemplateService _templates;

        public MessagingOtpService(
            IUserMessagingLinkRepository links,
            IMessagingOtpRepository otps,
            IMessagingSendLogRepository sendLogs,
            IEnumerable<IMessagingChannel> channels,
            ISmsSender sms = null,
            ISmsTemplateService templates = null)
        {
            _links = links;
            _otps = otps;
            _sendLogs = sendLogs;
            _channels = channels;
            _sms = sms;
            _templates = templates;
        }

        /// <summary>کد قالب پیامکی متناظر با هر purpose — با همین کد در ماژول «قالب‌های پیامک» پنل ادمین قابل تعریف/ویرایش است.</summary>
        private static string TemplateCodeFor(string purpose) => purpose switch
        {
            MessagingPurposes.Register => "Otp_Register",
            MessagingPurposes.OrderConfirm => "Otp_OrderConfirm",
            MessagingPurposes.IdentityVerification => "Otp_IdentityVerification",
            _ => "Otp_Generic"
        };

        public async Task<MessagingResult> SendOtpAsync(string phoneNumber, string purpose, string provider = null)
        {
            provider ??= MessagingProviders.Bale;
            purpose ??= MessagingPurposes.Register;

            var phone = NormalizePhone(phoneNumber);
            if (string.IsNullOrWhiteSpace(phone) || phone.Length < 10)
                return MessagingResult.Fail("شماره موبایل معتبر نیست.");

            var recent = await _otps.CountRecentAsync(provider, phone, 10);
            if (recent >= 5)
                return MessagingResult.Fail("تعداد درخواست کد زیاد است. چند دقیقه دیگر تلاش کنید.");

            var code = GenerateCode(6);
            var hash = HashCode(code);
            await _otps.InsertAsync(new MessagingOtp
            {
                Provider = provider,
                PhoneNumber = phone,
                Purpose = purpose,
                CodeHash = hash,
                ExpiresAt = DateTime.Now.AddMinutes(5)
            });

            var title = purpose switch
            {
                MessagingPurposes.Register => "ثبت‌نام",
                MessagingPurposes.OrderConfirm => "تأیید سفارش",
                _ => "تأیید"
            };

            if (string.Equals(provider, MessagingProviders.Sms, StringComparison.OrdinalIgnoreCase))
            {
                if (_sms == null)
                    return MessagingResult.Fail("سرویس پیامک در دسترس نیست.");

                var fallbackText = $"کد {title} کوچینو: {code}\nاین کد تا ۵ دقیقه معتبر است.";
                var (smsResult, logText) = await SendOtpSmsAsync(purpose, phone, code, fallbackText);
                await SafeLogAsync(provider, phone, null, MessagingMessageTypes.Otp, logText, smsResult.Ok, smsResult.Error);
                if (!smsResult.Ok)
                    return MessagingResult.Fail(string.IsNullOrWhiteSpace(smsResult.Error) ? "ارسال پیامک ناموفق بود." : smsResult.Error);
                return MessagingResult.Ok("کد تأیید با پیامک ارسال شد.");
            }

            var link = await _links.GetByPhoneAsync(provider, phone);
            if (link == null || string.IsNullOrWhiteSpace(link.ChatId))
                return MessagingResult.Fail(
                    string.Equals(provider, MessagingProviders.Telegram, StringComparison.OrdinalIgnoreCase)
                        ? "ابتدا در بات تلگرام /start بزنید و شماره موبایل خود را ارسال کنید."
                        : "ابتدا در بات بله دستور /start را بزنید و شماره موبایل خود را ارسال کنید تا حسابتان متصل شود.");

            var channel = _channels.FirstOrDefault(c => string.Equals(c.Provider, provider, StringComparison.OrdinalIgnoreCase));
            if (channel == null)
                return MessagingResult.Fail($"کانال ارسال «{provider}» فعال نیست.");

            var textBot = $"کد {title} کوچینو:\n<code>{code}</code>\n\nاین کد تا ۵ دقیقه معتبر است.";
            var (ok, error) = await channel.SendTextAsync(link.ChatId, textBot);
            await SafeLogAsync(provider, phone, link.ChatId, MessagingMessageTypes.Otp, textBot, ok, error);
            if (!ok)
                return MessagingResult.Fail(string.IsNullOrWhiteSpace(error) ? "ارسال پیام ناموفق بود." : error);

            return MessagingResult.Ok(
                string.Equals(provider, MessagingProviders.Telegram, StringComparison.OrdinalIgnoreCase)
                    ? "کد تأیید به بات تلگرام شما ارسال شد."
                    : "کد تأیید به بات بله شما ارسال شد.");
        }

        private async Task<(SmsSendResult Result, string LogText)> SendOtpSmsAsync(string purpose, string phone, string code, string fallbackText)
        {
            SmsTemplate template = null;
            if (_templates != null)
            {
                try { template = await _templates.GetByCodeAsync(TemplateCodeFor(purpose)); }
                catch { template = null; }
            }

            if (template != null && template.IsActive && template.UsesMeliSharedApi)
            {
                var result = await _sms.SendMeliSharedAsync(
                    phone,
                    template.MeliPayamakBodyId!.Value,
                    new[] { code },
                    null,
                    MessagingMessageTypes.Otp,
                    TemplateCodeFor(purpose));
                return (result, $"[shared] {fallbackText}");
            }

            if (template != null && template.IsActive && template.UsesVerifyApi)
            {
                var result = await _sms.SendVerifyAsync(
                    phone,
                    template.SmsIrTemplateId!.Value,
                    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["CODE"] = code, ["Code"] = code },
                    MessagingMessageTypes.Otp,
                    TemplateCodeFor(purpose));
                return (result, $"[verify] {fallbackText}");
            }

            var text = template != null && template.IsActive && !string.IsNullOrWhiteSpace(template.BodyTemplate)
                ? _templates.Render(template.BodyTemplate, new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["CODE"] = code })
                : fallbackText;

            return (await _sms.SendAsync(phone, text, null, MessagingMessageTypes.Otp, TemplateCodeFor(purpose)), text);
        }

        public async Task<MessagingResult> VerifyOtpAsync(string phoneNumber, string purpose, string code, string provider = null)
        {
            provider ??= MessagingProviders.Bale;
            purpose ??= MessagingPurposes.Register;

            var phone = NormalizePhone(phoneNumber);
            var cleanCode = (code ?? "").Trim();
            if (string.IsNullOrWhiteSpace(phone) || string.IsNullOrWhiteSpace(cleanCode))
                return MessagingResult.Fail("شماره یا کد وارد نشده است.");

            var otp = await _otps.GetActiveAsync(provider, phone, purpose);
            if (otp == null)
                return MessagingResult.Fail("کد معتبر یافت نشد یا منقضی شده است. دوباره درخواست کد دهید.");

            if (otp.AttemptCount >= 5)
                return MessagingResult.Fail("تعداد تلاش بیش از حد است. کد جدید بگیرید.");

            await _otps.IncrementAttemptAsync(otp.Id);

            var expectedHash = otp.CodeHash ?? "";
            var actualHash = HashCode(cleanCode);
            if (!FixedTimeHexEquals(expectedHash, actualHash))
                return MessagingResult.Fail("کد وارد شده نادرست است.");

            await _otps.MarkUsedAsync(otp.Id);
            return MessagingResult.Ok("کد تأیید شد.");
        }

        public async Task<MessagingResult> SendMessageAsync(string phoneNumber, string text, string provider = null)
        {
            provider ??= MessagingProviders.Bale;
            var phone = NormalizePhone(phoneNumber);
            if (string.IsNullOrWhiteSpace(phone) || string.IsNullOrWhiteSpace(text))
                return MessagingResult.Fail("شماره یا متن پیام نامعتبر است.");

            var link = await _links.GetByPhoneAsync(provider, phone);
            if (link == null)
                return MessagingResult.Fail("این شماره هنوز به بات متصل نشده است.");

            var channel = _channels.FirstOrDefault(c => string.Equals(c.Provider, provider, StringComparison.OrdinalIgnoreCase));
            if (channel == null)
                return MessagingResult.Fail($"کانال «{provider}» فعال نیست.");

            var (ok, error) = await channel.SendTextAsync(link.ChatId, text);
            await SafeLogAsync(provider, phone, link.ChatId, MessagingMessageTypes.Text, text, ok, error);
            return ok ? MessagingResult.Ok() : MessagingResult.Fail(error ?? "ارسال ناموفق");
        }

        private async Task SafeLogAsync(string provider, string phone, string chatId, string messageType, string text, bool success, string error)
        {
            try
            {
                await _sendLogs.InsertAsync(new MessagingSendLog
                {
                    Provider = provider,
                    PhoneNumber = phone,
                    ChatId = chatId,
                    MessageType = messageType,
                    MessageText = text ?? "",
                    Success = success,
                    ErrorMessage = success ? null : error
                });
            }
            catch
            {
                // لاگ نباید ارسال را خراب کند
            }
        }

        public static string NormalizePhone(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "";
            var sb = new StringBuilder(input.Length);
            foreach (var c in input)
            {
                if (c >= '۰' && c <= '۹') sb.Append((char)('0' + (c - '۰')));
                else if (c >= '٠' && c <= '٩') sb.Append((char)('0' + (c - '٠')));
                else if (char.IsDigit(c)) sb.Append(c);
            }
            var digits = sb.ToString();
            if (digits.StartsWith("98") && digits.Length == 12)
                digits = "0" + digits.Substring(2);
            if (digits.Length == 10 && digits.StartsWith("9"))
                digits = "0" + digits;
            return digits;
        }

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
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes("koochino-otp:" + code.Trim()));
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
    }
}
