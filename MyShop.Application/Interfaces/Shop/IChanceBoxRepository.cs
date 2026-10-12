using System.Collections.Generic;
using System.Threading.Tasks;
using MyShop.Core.Models;
using MyShop.Core.Models.Dto;

namespace MyShop.Application.Interfaces
{
    public interface IChanceBoxRepository
    {
        Task<int> CreateCodeAsync(ChanceBoxCode code);

        Task<ChanceBoxCode> GetByCodeAsync(string code);

        Task<ChanceBoxCode> GetLastForStoreAsync(int storeId);

        Task<List<ChanceBoxCode>> GetByStoreOwnerUserIdAsync(int storeOwnerUserId);

        Task<(bool Success, int ChanceBoxCodeUsageId)> MarkUsedAsync(int chanceBoxCodeId, int userId, long purchaseAmount);
        Task SetChanceBoxCodeUsageOrderIdAsync(int chanceBoxCodeUsageId, int orderId);

        Task<List<ChanceBoxCodeUsage>> GetUsagesByStoreOwnerUserIdAsync(int storeOwnerUserId);

        /// <summary>لیست فروشگاه‌هایی که جعبه شانس تعریف کرده‌اند (ادمین)</summary>
        Task<List<AdminChanceBoxStoreDto>> GetAdminStoresWithChanceBoxAsync(string? search);

        /// <summary>استفاده‌های جعبه شانس برای یک فروشگاه با توضیحات (ادمین)</summary>
        Task<List<ChanceBoxUsageDetailDto>> GetUsagesByStoreIdAsync(int storeId);
    }
}
