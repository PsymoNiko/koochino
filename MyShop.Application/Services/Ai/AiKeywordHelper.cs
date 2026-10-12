using System;
using System.Linq;

namespace MyShop.Application.Services.Ai
{
    internal static class AiKeywordHelper
    {
        /// <summary>
        /// Both sides are normalized because callers receive text that has already been folded
        /// (Arabic yeh/kaf, ZWNJ, digits), while the keyword literals below are written in plain Persian.
        /// </summary>
        public static bool Has(string text, params string[] words)
        {
            var haystack = PersianTextScorer.Normalize(text);
            if (haystack.Length == 0) return false;
            return words.Any(w =>
            {
                var needle = PersianTextScorer.Normalize(w);
                return needle.Length > 0 && haystack.IndexOf(needle, StringComparison.Ordinal) >= 0;
            });
        }

        public static bool IsDefinitionQuery(string text) =>
            Has(text,
                "چیست", "چیه", "چگونه", "چطور", "چجوری", "یعنی", "معنی", "فایده", "مزیت",
                "تفاوت", "کجا", "چرا", "آیا", "میتوان", "می‌شود", "میشه", "چطوری", "how", "what");

        public static bool IsPersonalQuery(string text) =>
            Has(text,
                "من ", " من", "دارم", "موجود", "چند", "وضعیت", "لیست", "کدهای من", "کد های من",
                "کوپن من", "امتیاز من", "ماموریت من", "مأموریت من", "my ");
    }
}
