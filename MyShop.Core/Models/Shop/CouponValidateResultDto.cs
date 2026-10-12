namespace MyShop.Core.Models.Dto
{
    /// <summary>
    /// نتیجه اعتبارسنجی کوپن قبل از تایید کاربر
    /// </summary>
    public class CouponValidateResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public long DiscountAmount { get; set; }
        public int CoponID { get; set; }
    }
}
