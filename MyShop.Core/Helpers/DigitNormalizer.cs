using System.Text;

namespace MyShop.Core.Helpers
{
    /// <summary>
    /// تبدیل ارقام فارسی/عربی به انگلیسی و حذف هر کاراکتر غیر رقمی — برای فیلدهایی مثل کد ملی، کد پستی و کد پیامکی
    /// که باید فقط شامل رقم ASCII باشند (چون HTML pattern="[0-9]*" فقط با ارقام انگلیسی مطابقت دارد).
    /// </summary>
    public static class DigitNormalizer
    {
        public static string ToAsciiDigitsOnly(string input)
        {
            if (string.IsNullOrEmpty(input)) return "";

            var sb = new StringBuilder(input.Length);
            foreach (var c in input)
            {
                if (c >= '0' && c <= '9') sb.Append(c);
                else if (c >= '۰' && c <= '۹') sb.Append((char)('0' + (c - '۰')));
                else if (c >= '٠' && c <= '٩') sb.Append((char)('0' + (c - '٠')));
            }
            return sb.ToString();
        }

        /// <summary>کد ملی: فقط رقم انگلیسی، حداکثر ۱۰ رقم — برای استفاده در oninput فیلدهای کد ملی.</summary>
        public static string ToNationalCodeInput(string input)
        {
            var digits = ToAsciiDigitsOnly(input);
            return digits.Length > 10 ? digits.Substring(0, 10) : digits;
        }
    }

    /// <summary>
    /// حذف هر کاراکتر غیر از حروف فارسی/عربی، فاصله و نیم‌فاصله — برای فیلدهایی مثل نام و نام خانوادگی
    /// که باید فقط فارسی باشند (حروف لاتین/انگلیسی و ارقام رد می‌شوند).
    /// </summary>
    public static class PersianTextNormalizer
    {
        public static string PersianLettersOnly(string input)
        {
            if (string.IsNullOrEmpty(input)) return "";

            var sb = new StringBuilder(input.Length);
            foreach (var c in input)
            {
                if ((c >= '؀' && c <= 'ۿ') // بلوک عربی/فارسی (شامل ارقام فارسی هم می‌شود، پایین حذف می‌شوند)
                    || c == '‌' // نیم‌فاصله (ZWNJ)
                    || c == ' ')
                {
                    if (c >= '۰' && c <= '۹') continue; // ارقام فارسی در نام مجاز نیستند
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }
    }
}
