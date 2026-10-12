using System;

namespace MyShop.Core.Models
{
    /// <summary>
    /// رکورد استفاده یک کاربر از یک کوپن (هر کاربر فقط یک بار per کوپن)
    /// </summary>
    public class CouponUsage
    {
        public int Id { get; set; }
        public int CoponID { get; set; }
        public int UserId { get; set; }
        public DateTime UsedAt { get; set; }
        public long PurchaseAmount { get; set; }
        public long DiscountAmount { get; set; }
        public string CouponCode { get; set; } = string.Empty;
        public string CouponTitle { get; set; } = string.Empty;
        public string StoreName { get; set; } = string.Empty;
    }
}
