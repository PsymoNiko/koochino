using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace MyShop.Core.Helpers
{
    /// <summary>
    /// تبدیل خطاهای فنی به پیام‌های قابل‌نمایش برای کاربر (فارسی).
    /// </summary>
    public static class UserFacingErrorHelper
    {
        public static string Format(Exception ex, string prefix = null)
        {
            var message = ToPersianError(ex);
            return string.IsNullOrWhiteSpace(prefix) ? message : $"{prefix.TrimEnd(' ', ':', '.')} {message}";
        }

        public static string ToPersianError(Exception ex)
        {
            if (ex is HttpRequestException)
                return "خطا در ارتباط با سرور. لطفاً دوباره تلاش کنید.";

            if (ex is TaskCanceledException or OperationCanceledException)
                return "زمان درخواست به پایان رسید. دوباره تلاش کنید.";

            if (ex is UnauthorizedAccessException)
                return "دسترسی مجاز نیست.";

            return ToPersianError(ex?.Message);
        }

        public static string ToPersianError(string message)
        {
            var m = message?.Trim() ?? "";
            if (string.IsNullOrEmpty(m)) return "خطا در عملیات.";
            if (IsInvalidOrCorruptedMessage(m)) return "خطا در عملیات.";

            if (m.Contains("not found", StringComparison.OrdinalIgnoreCase))
                return "اطلاعات مورد نظر یافت نشد.";

            if (m.Contains("timeout", StringComparison.OrdinalIgnoreCase)
                || m.Contains("timed out", StringComparison.OrdinalIgnoreCase))
                return "زمان درخواست به پایان رسید. دوباره تلاش کنید.";

            if (m.Contains("network", StringComparison.OrdinalIgnoreCase)
                || m.Contains("connection", StringComparison.OrdinalIgnoreCase)
                || m.Contains("refused", StringComparison.OrdinalIgnoreCase))
                return "خطا در ارتباط با سرور. لطفاً دوباره تلاش کنید.";

            if (m.Contains("unauthorized", StringComparison.OrdinalIgnoreCase)
                || m.Contains("forbidden", StringComparison.OrdinalIgnoreCase))
                return "دسترسی مجاز نیست.";

            if (m.Contains("503") || m.Contains("service unavailable", StringComparison.OrdinalIgnoreCase))
                return "سرویس موقتاً در دسترس نیست. لطفاً بعداً تلاش کنید.";

            return "خطا: " + m;
        }

        public static string NormalizeMessage(string msg)
        {
            if (string.IsNullOrWhiteSpace(msg)) return "—";
            var t = msg.Trim();
            if (t.Length == 0) return "—";
            if (IsInvalidOrCorruptedMessage(t)) return "پیام نامشخص";
            return msg;
        }

        public static bool IsInvalidOrCorruptedMessage(string msg)
        {
            if (string.IsNullOrWhiteSpace(msg) || msg.Trim().Length == 0) return true;
            const char replacementChar = '\uFFFD';
            var t = msg.Trim();
            var invalidCount = t.Count(c => c == '?' || c == replacementChar || char.IsControl(c));
            return invalidCount == t.Length || invalidCount >= (t.Length + 1) / 2;
        }
    }
}
