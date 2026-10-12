using MyShop.Core.Models;
using MyShop.Core.Models.Security;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Interfaces
{
    public interface IStoreRepository
    {
        Task<int> CountAsync();
        Task<List<Store>> LatestAsync(int count);
        /// <summary>پر فروش‌ترین فروشگاه‌های فعال (بر اساس تعداد سفارش)، حداکثر count تا.</summary>
        Task<List<Store>> TopSellingAsync(int count);
        Task<List<Store>> GetByUserIdAsync(int userId);
        Task<Store> GetByIdAsync(int storeId);
        Task<int> CreateAsync(Store store);
        Task<bool> UpdateAsync(Store store);
        Task<(bool CanDelete, List<string> Reasons)> GetDeleteBlockersAsync(int storeId);
        Task<bool> DeleteAsync(int storeId);
        Task<bool> ToggleStatusAsync(int storeId, bool status);
        Task<List<Store>> GetAllAsync();
        Task<List<Store>> GetAllScopedAsync(DataScopeQuery scope);
        Task<int> CountScopedAsync(DataScopeQuery scope);
    }
}
