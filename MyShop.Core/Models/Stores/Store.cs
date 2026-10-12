using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyShop.Core.Models
{
    public class Store
    {
        public int Id { get; set; }
        public int StoreID { get; set; }
        public int? CompanyId { get; set; }
        public int SellerId { get; set; }
        public int UserID { get; set; }
        public string StoreName { get; set; } = string.Empty;
        public string Description { get; set; }
        public string StoreDescription { get; set; }
        public string Address { get; set; }
        public string StoreAddress { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime StoreCreatedAt { get; set; }
        public string StoreFamilyName { get; set; }
        public int? StoreCity { get; set; }
        public string StorePostCode { get; set; }
        public string StorePhone { get; set; }
        public string StorePos1 { get; set; }
        public string StorePos2 { get; set; }
        public string StorePos3 { get; set; }
        public bool StoreStat { get; set; } // 0 = تحت بررسی، 1 = فعال
        
        // فیلدهای جدید برای نقشه و تصویر
        /// <summary>
        /// عرض جغرافیایی (Latitude) برای نمایش روی نقشه
        /// </summary>
        public decimal? StoreLatitude { get; set; }
        
        /// <summary>
        /// طول جغرافیایی (Longitude) برای نمایش روی نقشه
        /// </summary>
        public decimal? StoreLongitude { get; set; }
        
        /// <summary>
        /// آدرس تصویر پروفایل فروشگاه
        /// </summary>
        public string StoreProfileImageUrl { get; set; }
        
        /// <summary>
        /// ساعت شروع کار
        /// </summary>
        public string StoreOpeningHours { get; set; }
        
        /// <summary>
        /// ساعت پایان کار
        /// </summary>
        public string StoreClosingHours { get; set; }
        
        /// <summary>
        /// روزهای هفته که فروشگاه باز است (JSON یا comma-separated)
        /// </summary>
        public string StoreWorkingDays { get; set; }
        
        /// <summary>
        /// وب‌سایت فروشگاه
        /// </summary>
        public string StoreWebsite { get; set; }
        
        /// <summary>
        /// شبکه‌های اجتماعی (JSON)
        /// </summary>
        public string StoreSocialMedia { get; set; }
        
        /// <summary>
        /// امتیاز فروشگاه (برای رتبه‌بندی)
        /// </summary>
        public decimal? StoreRating { get; set; }
        
        /// <summary>
        /// تعداد نظرات
        /// </summary>
        public int StoreReviewCount { get; set; }
        
        /// <summary>
        /// آیا فروشگاه روی نقشه فعال است
        /// </summary>
        public bool IsLocationEnabled { get; set; }

        /// <summary>دسته‌بندی فروشگاه</summary>
        public int? CategoryId { get; set; }
        public string CategoryName { get; set; }

        /// <summary>سایت (برند) فروشگاه</summary>
        public int? SiteId { get; set; }
    }
}
