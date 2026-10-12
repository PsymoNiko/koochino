using System;

namespace MyShop.Core.Models
{
    /// <summary>
    /// تنظیمات سئو هر صفحه (قابل مدیریت از پنل ادمین)
    /// </summary>
    public class PageSeo
    {
        public int Id { get; set; }
        /// <summary>کلید یکتا صفحه مثلاً home, stores, store, post</summary>
        public string PageKey { get; set; } = string.Empty;
        /// <summary>نام نمایشی در پنل ادمین</summary>
        public string PageName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string MetaDescription { get; set; } = string.Empty;
        public string MetaKeywords { get; set; } = string.Empty;
        public string OgTitle { get; set; } = string.Empty;
        public string OgDescription { get; set; } = string.Empty;
        public string OgImage { get; set; } = string.Empty;
        public string CanonicalUrl { get; set; } = string.Empty;
        public DateTime? UpdatedAt { get; set; }
    }
}
