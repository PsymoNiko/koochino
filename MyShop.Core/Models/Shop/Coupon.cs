using System;

namespace MyShop.Core.Models
{
    public class Coupon
    {
        public int Id { get; set; }
        public int CoponID { get; set; }
        public string CoponTitle { get; set; } = string.Empty;
        public string CoponCode { get; set; } = string.Empty;
        public DateTime CoponInsertDate { get; set; }
        public DateTime CoponUpdateDate { get; set; }
        /// <summary>تاریخ شروع اعتبار کوپن (اختیاری؛ اگر null باشد از زمان ایجاد معتبر است)</summary>
        public DateTime? CoponStartDate { get; set; }
        public DateTime CoponExpireDate { get; set; }
        public int CoponStock { get; set; }
        public string CoponMinBuy { get; set; } = string.Empty;
        public string CoponMaxBuy { get; set; } = string.Empty;
        public int StoreID { get; set; }
        public int UserID { get; set; }
        public bool CoponStatStor { get; set; }
        public bool CoponStatAdmin { get; set; }
        /// <summary>0 = مبلغ ثابت، 1 = درصد</summary>
        public int DiscountType { get; set; }
        /// <summary>مبلغ تخفیف (تومان) یا درصد (۱–۱۰۰)</summary>
        public long? DiscountValue { get; set; }
        public decimal? DiscountPercent { get; set; }
        public decimal? DiscountAmount { get; set; }
        
        // Navigation properties
        public string StoreName { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
    }
}
