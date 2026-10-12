using System;

namespace MyShop.Core.Models
{
    /// <summary>
    /// ماموریت‌های امتیازدهی که توسط ادمین تعریف می‌شوند
    /// </summary>
    public class PointMission
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string MissionType { get; set; } // "PurchaseRating", "StoreComment", "PurchaseAmount", "Custom"
        public int Points { get; set; }
        /// <summary>ترتیب نمایش در پنل کاربر (۱ = اول، صعودی)</summary>
        public int MissionSort { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? CreatedByUserId { get; set; }
        public int? SiteId { get; set; }
        public string SiteName { get; set; }
        public string SiteCode { get; set; }
        
        // برای ماموریت‌های PurchaseAmount
        public decimal? MinPurchaseAmount { get; set; }
        public decimal? MaxPurchaseAmount { get; set; }
        
        // برای ماموریت‌های Custom
        public string CustomConfig { get; set; } // JSON برای تنظیمات خاص
        
        // لینک هدایت هنگام کلیک روی "مشاهده جزئیات"
        public string RedirectUrl { get; set; }
        
        // نام Stored Procedure برای چک کردن شرایط دریافت امتیاز
        public string ValidationSP { get; set; }
        
        // محدودیت تعداد دریافت (0 = نامحدود)
        public int MaxClaimCount { get; set; }
        
        // تاریخ شروع ماموریت
        public DateTime? StartDate { get; set; }
        
        // تاریخ پایان ماموریت
        public DateTime? EndDate { get; set; }
    }
}
