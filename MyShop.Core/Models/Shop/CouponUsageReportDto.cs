using System;

namespace MyShop.Core.Models.Dto
{
    /// <summary>
    /// یک رکورد گزارش استفاده از کوپن (ادمین)
    /// </summary>
    public class CouponUsageReportDto
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
        public string UserName { get; set; } = string.Empty;
        public string UserPhone { get; set; } = string.Empty;
    }
}
