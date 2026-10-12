using System;

namespace MyShop.Core.Models
{
    /// <summary>
    /// مدل مدارک فروشگاه
    /// </summary>
    public class StoreDocument
    {
        public int DocumentId { get; set; }
        public int StoreId { get; set; }
        
        /// <summary>
        /// عنوان سند
        /// </summary>
        public string Title { get; set; } = string.Empty;
        
        /// <summary>
        /// نوع سند (جواز کسب، اجاره نامه، و غیره)
        /// </summary>
        public string DocumentType { get; set; } = string.Empty;
        
        /// <summary>
        /// مسیر فایل تصویر
        /// </summary>
        public string ImageUrl { get; set; } = string.Empty;
        
        /// <summary>
        /// توضیحات
        /// </summary>
        public string Description { get; set; }
        
        /// <summary>
        /// تاریخ آپلود
        /// </summary>
        public DateTime UploadedAt { get; set; }
        
        /// <summary>
        /// تاریخ انقضا (برای مدارکی مثل جواز کسب)
        /// </summary>
        public DateTime? ExpiryDate { get; set; }
        
        /// <summary>
        /// وضعیت تایید (توسط ادمین)
        /// </summary>
        public bool IsApproved { get; set; }
        
        /// <summary>
        /// تاریخ تایید
        /// </summary>
        public DateTime? ApprovedAt { get; set; }
        
        /// <summary>
        /// شناسه کاربر تاییدکننده (ادمین)
        /// </summary>
        public int? ApprovedBy { get; set; }
        
        /// <summary>
        /// حجم فایل به بایت
        /// </summary>
        public long FileSize { get; set; }
        
        /// <summary>
        /// نوع MIME فایل
        /// </summary>
        public string MimeType { get; set; }
        
        /// <summary>
        /// ترتیب نمایش (برای گالری)
        /// </summary>
        public int DisplayOrder { get; set; }
    }
}
