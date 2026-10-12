using System;

namespace MyShop.Core.Models.Dto
{
    /// <summary>
    /// گزارش استفاده از کوپن برای پنل فروشنده (با وضعیت تایید)
    /// </summary>
    public class CouponUsageStoreOwnerDto
    {
        public int Id { get; set; }
        public int CoponID { get; set; }
        public int UserId { get; set; }
        public DateTime UsedAt { get; set; }
        public long PurchaseAmount { get; set; }
        public long DiscountAmount { get; set; }
        public int? OrderId { get; set; }
        public bool? ApprovedByStore { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string CouponCode { get; set; } = string.Empty;
        public string CouponTitle { get; set; } = string.Empty;
        public string StoreName { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string UserPhone { get; set; } = string.Empty;
    }
}
