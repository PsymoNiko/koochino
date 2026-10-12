using System;
using System.Collections.Generic;

namespace MyShop.Core.Models.Sites
{
    public class Site
    {
        public int Id { get; set; }
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public bool IsActive { get; set; } = true;
        public string BrandJson { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<SiteDomain> Domains { get; set; } = new();
    }

    public class SiteDomain
    {
        public int Id { get; set; }
        public int SiteId { get; set; }
        public string Host { get; set; } = "";
        public bool IsPrimary { get; set; }
    }

    /// <summary>Resolved site for the current HTTP request (cached).</summary>
    public class ResolvedSite
    {
        public int SiteId { get; set; }
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public string BrandJson { get; set; }
        public string Host { get; set; } = "";
        public bool IsActive { get; set; } = true;
    }

    public class SiteBrandCopy
    {
        public string SiteName { get; set; }
        public string PageTitle { get; set; }
        public string HeroBadge { get; set; }
        public string HeroTitleHtml { get; set; }
        public string HeroSubtitle { get; set; }
        public string FooterText { get; set; }

        /// <summary>لوگو هدر / UI (معمولاً تا ۵۱۲×۵۱۲).</summary>
        public string LogoUrl { get; set; }
        /// <summary>فاوآیکون تب مرورگر (۳۲×۳۲ PNG یا .ico).</summary>
        public string FaviconUrl { get; set; }
        /// <summary>آیکون iOS / افزودن به صفحه اصلی (۱۸۰×۱۸۰).</summary>
        public string AppleTouchIconUrl { get; set; }
        public string PwaIcon192Url { get; set; }
        public string PwaIcon512Url { get; set; }
        /// <summary>تصویر اشتراک‌گذاری شبکه‌های اجتماعی (۱۲۰۰×۶۳۰) — اختیاری.</summary>
        public string OgImageUrl { get; set; }
        /// <summary>رنگ اصلی اختیاری داخل BrandJson — بدون جدول جدا.</summary>
        public string PrimaryColor { get; set; }
        public string SecondaryColor { get; set; }
    }
}
