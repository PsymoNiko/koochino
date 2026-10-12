using System;

namespace MyShop.Core.Models.Dto
{
    /// <summary>
    /// جزئیات یک استفاده از جعبه شانس (شامل توضیحات جعبه) — برای ادمین
    /// </summary>
    public class ChanceBoxUsageDetailDto
    {
        public int Id { get; set; }
        public int ChanceBoxCodeId { get; set; }
        public int StoreId { get; set; }
        public int UserId { get; set; }
        public long PurchaseAmount { get; set; }
        public DateTime UsedAt { get; set; }
        public string Code { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? PrizeDescription { get; set; }
        public string StoreName { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
    }
}
