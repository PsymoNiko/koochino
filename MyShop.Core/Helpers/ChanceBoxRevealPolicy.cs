using System;

namespace MyShop.Core.Helpers
{
    /// <summary>تصمیم نمایش افکت جعبه گنج برای کد جعبه‌شانس — بدون وابستگی به UI.</summary>
    public static class ChanceBoxRevealPolicy
    {
        public const string DefaultPrizeText = "جایزه شما توسط فروشگاه اعلام می‌شود";

        /// <summary>کد را برای مقایسه و جستجو یکدست می‌کند (ارقام فارسی/عربی به انگلیسی).</summary>
        public static string NormalizeCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return "";
            var trimmed = code.Trim();
            var chars = new char[trimmed.Length];
            for (var i = 0; i < trimmed.Length; i++)
            {
                var c = trimmed[i];
                if (c >= '۰' && c <= '۹') chars[i] = (char)('0' + (c - '۰'));
                else if (c >= '٠' && c <= '٩') chars[i] = (char)('0' + (c - '٠'));
                else chars[i] = c;
            }
            return new string(chars);
        }

        /// <summary>
        /// افکت تنها وقتی اجرا می‌شود که کد معتبر و استفاده‌نشده باشد و قبلاً برای همان کد نمایش داده نشده باشد.
        /// </summary>
        public static bool ShouldReveal(bool found, bool isUsed, string code, string lastRevealedCode)
        {
            if (!found || isUsed) return false;

            var current = NormalizeCode(code);
            if (current.Length == 0) return false;

            return !string.Equals(current, NormalizeCode(lastRevealedCode), StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>توضیح جایزه فروشنده اختیاری است؛ در نبود آن متن جایگزین نمایش داده می‌شود.</summary>
        public static string PrizeText(string prizeDescription)
            => string.IsNullOrWhiteSpace(prizeDescription) ? DefaultPrizeText : prizeDescription.Trim();
    }
}
