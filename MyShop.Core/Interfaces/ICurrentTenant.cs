namespace MyShop.Core.Interfaces
{
    /// <summary>
    /// اطلاعات tenant/site جاری در این درخواست.
    /// Host → ConnectionString + SiteId (shared DB multi-site).
    /// </summary>
    public interface ICurrentTenant
    {
        /// <summary>کلید host (مثلاً koochino.shop).</summary>
        string TenantKey { get; }

        /// <summary>رشته اتصال دیتابیس این درخواست.</summary>
        string ConnectionString { get; }

        /// <summary>شناسه سایت resolve‌شده از Host (۰ اگر ناشناخته).</summary>
        int SiteId { get; }

        /// <summary>کد سایت (مثلاً koochino)</summary>
        string SiteCode { get; }

        /// <summary>نام نمایشی سایت</summary>
        string SiteName { get; }

        /// <summary>آیا connection string معتبر است.</summary>
        bool IsResolved { get; }

        /// <summary>آیا SiteId معتبر (&gt; 0) است.</summary>
        bool HasSite { get; }
    }
}
