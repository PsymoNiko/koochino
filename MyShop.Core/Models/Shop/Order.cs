using System;

namespace MyShop.Core.Models
{
    /// <summary>
    /// سفارش/خرید — جدول مرکزی همه انواع خرید (عادی، کوپن، جعبه‌شانس)
    /// </summary>
    public class Order
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public int UserId { get; set; }
        public int? StoreId { get; set; }
        /// <summary>0=عادی, 1=با کوپن, 2=جعبه شانس</summary>
        public byte OrderType { get; set; }
        /// <summary>0=آنلاین, 1=حضوری</summary>
        public byte SaleChannel { get; set; }
        public long TotalAmount { get; set; }
        public long DiscountAmount { get; set; }
        public long FinalAmount { get; set; }
        /// <summary>مبلغ خرج‌شده از کیف ریالی مشتری روی این سفارش (جدا از تخفیف کوپن).</summary>
        public long? WalletAmountApplied { get; set; }
        public int? CouponUsageId { get; set; }
        public int? ChanceBoxCodeUsageId { get; set; }
        public DateTime OrderDate { get; set; }
        /// <summary>0=در حال بررسی, 1=تایید شده, 2=رد شده</summary>
        public byte Status { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        // برای نمایش در لیست
        public string? CustomerName { get; set; }
        public string? CustomerPhone { get; set; }
        public string? StoreName { get; set; }
        public string? CouponCode { get; set; }
        public string? OrderTypeTitle { get; set; }
        public string? StatusTitle { get; set; }
        /// <summary>سایت (برند) سفارش</summary>
        public int? SiteId { get; set; }
    }
}
