namespace MyShop.Core.Models.Dto
{
    /// <summary>
    /// یک فروشگاه که جعبه شانس تعریف کرده — برای لیست ادمین
    /// </summary>
    public class AdminChanceBoxStoreDto
    {
        public int StoreId { get; set; }
        public string StoreName { get; set; } = string.Empty;
        public string OwnerName { get; set; } = string.Empty;
        public int TotalCodes { get; set; }
        public int UsageCount { get; set; }
        public string? SampleCode { get; set; }
    }
}
