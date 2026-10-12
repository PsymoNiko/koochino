using System;

namespace MyShop.Core.Models
{
    /// <summary>
    /// لاگ نصب PWA — هر بار کاربر اپ را روی گوشی/دسکتاپ نصب کند یک رکورد ثبت می‌شود.
    /// توجه: در مرورگر امکان خواندن MAC Address وجود ندارد؛ این فیلد برای سیستم‌های دیگر اختیاری است.
    /// </summary>
    public class PwaInstallLog
    {
        public int Id { get; set; }
        /// <summary>کاربر لاگین‌شده (در صورت نصب بدون لاگین null)</summary>
        public int? UserId { get; set; }
        public DateTime InstalledAt { get; set; }
        public string IpAddress { get; set; }
        public string UserAgent { get; set; }
        /// <summary>نوع دستگاه/مرورگر از User-Agent یا client</summary>
        public string DeviceType { get; set; }
        public string Platform { get; set; }
        public string Language { get; set; }
        public int? ScreenWidth { get; set; }
        public int? ScreenHeight { get; set; }
        /// <summary>تعداد هسته CPU (در صورت وجود)</summary>
        public int? HardwareConcurrency { get; set; }
        /// <summary>حافظه دستگاه به GB (در صورت وجود)</summary>
        public decimal? DeviceMemory { get; set; }
        public string TimeZone { get; set; }
        /// <summary>اطلاعات اضافی JSON (مثلاً vendor, model در صورت ارسال از کلاینت)</summary>
        public string ExtraInfo { get; set; }
        /// <summary>در مرورگر/PWA قابل خواندن نیست؛ برای سیستم‌های دیگر</summary>
        public string MacAddress { get; set; }
    }
}
