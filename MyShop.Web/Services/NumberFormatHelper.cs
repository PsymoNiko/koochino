using System.Globalization;
using System.Linq;
using System.Text;

namespace MyShop.Web.Services
{
    /// <summary>
    /// فرمت یکسان اعداد در کل سایت: جداکننده سه‌رقمی (هزارگان) — مثال: 1,234,567
    /// </summary>
    public static class NumberFormatHelper
    {
        /// <summary>
        /// ارقام فارسی (۰-۹) و عربی (٠-٩) را به انگلیسی (0-9) تبدیل می‌کند.
        /// </summary>
        public static string NormalizePhoneDigits(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            var sb = new StringBuilder(input.Length);
            foreach (char c in input)
            {
                if (c >= '۰' && c <= '۹') sb.Append((char)('0' + (c - '۰')));
                else if (c >= '٠' && c <= '٩') sb.Append((char)('0' + (c - '٠')));
                else sb.Append(c);
            }
            return sb.ToString();
        }

        /// <summary>نرمال‌سازی شماره موبایل ایران به فرمت 09xxxxxxxxx</summary>
        public static string ToIranMobile(string input)
        {
            var value = NormalizePhoneDigits(input ?? "").Trim().Replace(" ", "").Replace("-", "");
            if (value.StartsWith("+98")) value = "0" + value.Substring(3);
            else if (value.StartsWith("98") && value.Length >= 12) value = "0" + value.Substring(2);
            value = new string(value.Where(char.IsDigit).ToArray());
            if (value.StartsWith("9") && value.Length == 10) value = "0" + value;
            return value;
        }
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        /// <summary>عدد را با جداکننده هزارگان (سه‌تا سه‌تا) برمی‌گرداند.</summary>
        public static string Format(long value) => value.ToString("N0", Inv);

        /// <summary>عدد اعشاری با تعداد رقم اعشار مشخص.</summary>
        public static string Format(decimal value, int decimals = 0) =>
            decimals == 0 ? value.ToString("N0", Inv) : value.ToString("N" + decimals, Inv);

        /// <summary>رشته عددی را پارس و با جداکننده هزارگان فرمت می‌کند.</summary>
        public static string Format(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "";
            var digits = new string(value.Where(char.IsDigit).ToArray());
            if (string.IsNullOrEmpty(digits)) return value;
            return long.TryParse(digits, out long n) ? Format(n) : value;
        }
    }
}
