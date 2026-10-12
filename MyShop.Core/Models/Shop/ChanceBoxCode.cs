using System;

namespace MyShop.Core.Models
{
    /// <summary>
    /// کد جعبه شانس که توسط فروشنده برای یک فروشگاه تعریف می‌شود.
    /// </summary>
    public class ChanceBoxCode
    {
        public int Id { get; set; }
        public int ChanceBoxCodeId { get; set; }

        /// <summary>
        /// شناسه فروشگاه
        /// </summary>
        public int StoreId { get; set; }

        /// <summary>
        /// شناسه کاربر فروشنده که این کد را ساخته است
        /// </summary>
        public int StoreOwnerUserId { get; set; }

        /// <summary>
        /// کد کامل جعبه شانس (۴ رقم اول: شناسه فروشگاه، ۵ رقم بعدی: شماره متوالی)
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// حداقل مبلغ خرید برای استفاده از این کد (تومان)
        /// </summary>
        public long MinPurchaseAmount { get; set; }

        /// <summary>
        /// توضیح کلی جعبه شانس
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// توضیح جایزه
        /// </summary>
        public string PrizeDescription { get; set; } = string.Empty;

        /// <summary>
        /// تاریخ ثبت کد
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// آیا این کد قبلاً استفاده شده است یا خیر (هر کد تنها یک بار قابل استفاده است)
        /// </summary>
        public bool IsUsed { get; set; }

        /// <summary>
        /// در صورت استفاده شدن، شناسه کاربری که استفاده کرده
        /// </summary>
        public int? UsedByUserId { get; set; }

        /// <summary>
        /// مبلغ خریدی که موقع استفاده ثبت شده
        /// </summary>
        public long? UsedPurchaseAmount { get; set; }

        /// <summary>
        /// تاریخ و زمان استفاده از کد
        /// </summary>
        public DateTime? UsedAt { get; set; }

        // اطلاعات کمکی برای نمایش
        public string StoreName { get; set; } = string.Empty;
        public string? UsedByUserName { get; set; }
    }
}
