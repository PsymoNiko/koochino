using System;

namespace MyShop.Core.Models
{
    /// <summary>
    /// رکورد استفاده از یک کد جعبه شانس توسط مشتری
    /// </summary>
    public class ChanceBoxCodeUsage
    {
        public int Id { get; set; }
        public int ChanceBoxCodeId { get; set; }
        public int StoreId { get; set; }

        /// <summary>
        /// شناسه کاربر (مشتری) که کد را وارد کرده است
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// مبلغ خرید ثبت شده (تومان)
        /// </summary>
        public long PurchaseAmount { get; set; }

        /// <summary>
        /// تاریخ و زمان استفاده
        /// </summary>
        public DateTime UsedAt { get; set; }

        // اطلاعات کمکی برای نمایش در گزارش‌ها
        public string Code { get; set; } = string.Empty;
        public string StoreName { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
    }
}
