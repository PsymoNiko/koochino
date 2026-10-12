using System.Collections.Generic;
using System.Threading.Tasks;
using MyShop.Core.Models;
using MyShop.Core.Models.Dto;

namespace MyShop.Application.Interfaces
{
    public class UseChanceBoxResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int ChanceBoxCodeUsageId { get; set; }
        public int StoreId { get; set; }
    }

    public class ChanceBoxLookupResult
    {
        public bool Found { get; set; }
        public bool IsUsed { get; set; }
        public long MinPurchaseAmount { get; set; }
        public int StoreId { get; set; }
        public string Message { get; set; } = string.Empty;
        /// <summary>توضیح جایزه برای نمایش به مشتری هنگام تایید کد</summary>
        public string PrizeDescription { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public interface IChanceBoxService
    {
        /// <summary>
        /// ایجاد یک کد جعبه شانس جدید برای یک فروشگاه
        /// </summary>
        Task<string> CreateCodeAsync(int storeOwnerUserId, int storeId, long minPurchaseAmount, string description, string prizeDescription);

        /// <summary>
        /// پیش‌نمایش کد بدون مصرف: حداقل خرید و وضعیت استفاده
        /// </summary>
        Task<ChanceBoxLookupResult> LookupCodeAsync(string code);

        /// <summary>
        /// استفاده از یک کد جعبه شانس توسط مشتری
        /// </summary>
        Task<UseChanceBoxResult> UseCodeAsync(int customerUserId, int storeId, long purchaseAmount, string code);

        /// <summary>
        /// لیست کدهای جعبه شانس فروشنده
        /// </summary>
        Task<List<ChanceBoxCode>> GetCodesForStoreOwnerAsync(int storeOwnerUserId);

        /// <summary>
        /// لیست افرادی که کد را صحیح وارد کرده‌اند (مرتب شده از آخرین استفاده)
        /// </summary>
        Task<List<ChanceBoxCodeUsage>> GetUsagesForStoreOwnerAsync(int storeOwnerUserId);

        /// <summary>
        /// ادمین: لیست فروشگاه‌هایی که جعبه شانس تعریف کرده‌اند (با جستجو بر اساس نام فروشگاه یا کد)
        /// </summary>
        Task<List<AdminChanceBoxStoreDto>> GetAdminStoresWithChanceBoxAsync(string? search);

        /// <summary>
        /// ادمین: استفاده‌های جعبه شانس برای یک فروشگاه (با توضیحات جعبه)
        /// </summary>
        Task<List<ChanceBoxUsageDetailDto>> GetUsagesByStoreIdForAdminAsync(int storeId);
    }
}
