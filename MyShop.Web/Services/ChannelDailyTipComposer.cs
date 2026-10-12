using MyShop.Application.Services.Ai;
using System;
using System.Collections.Generic;

namespace MyShop.Web.Services
{
    /// <summary>تولید متن پست روزانه کانال از قالب‌های محلی (بدون API پولی).</summary>
    public static class ChannelDailyTipComposer
    {
        static readonly string[] FeatureTips =
        {
            "با ثبت خرید حضوری در کوچینو امتیاز بگیرید و در جوایز شرکت کنید.",
            "جعبه شانس کوچینو: بعد از خرید، شانس برنده شدن جایزه دارید.",
            "کد تخفیف خود را در پنل مشتری ببینید و در خرید بعدی استفاده کنید.",
            "با دعوت دوستان به کوچینو امتیاز بیشتری جمع کنید.",
            "مأموریت‌های فروشگاه را انجام دهید و امتیاز وفاداری بگیرید.",
            "پروفایل مشتری را کامل کنید تا از همه امکانات کوچینو استفاده کنید.",
            "فروشگاه‌های عضو را از صفحه فروشگاه‌ها پیدا کنید یا QR را اسکن کنید.",
            "از بات پیام‌رسان کوچینو برای دریافت کد تأیید و اعلان‌ها استفاده کنید.",
            "امتیازهایتان را برای رویدادها و جوایز ویژه نگه دارید.",
            "نظر خود را بعد از خرید ثبت کنید و در چرخه وفاداری مشارکت کنید."
        };

        public static string ComposeForDate(DateTime localDate)
        {
            var tipIndex = Math.Abs(localDate.DayOfYear) % FeatureTips.Length;
            var tip = FeatureTips[tipIndex];

            string kbSnippet = null;
            try
            {
                var faqs = KochinoKnowledgeBase.All;
                if (faqs != null && faqs.Count > 0)
                {
                    var faq = faqs[Math.Abs(localDate.DayOfYear * 7) % faqs.Count];
                    kbSnippet = Truncate(faq.Answer, 180);
                }
            }
            catch { /* knowledge optional */ }

            var lines = new List<string>
            {
                "✨ نکته روز کوچینو",
                "",
                tip,
                "",
                KochinoKnowledgeBase.Tagline
            };

            if (!string.IsNullOrWhiteSpace(kbSnippet))
            {
                lines.Add("");
                lines.Add("ℹ️ " + kbSnippet);
            }

            lines.Add("");
            lines.Add("🌐 koochino.shop");

            return string.Join("\n", lines);
        }

        static string Truncate(string s, int max)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            s = s.Trim();
            if (s.Length <= max) return s;
            return s.Substring(0, max - 1).TrimEnd() + "…";
        }
    }
}
