using MyShop.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Interfaces
{
    public interface IStoreService
    {
        Task<int> CountAsync();

        // متد جدید برای داشبورد
        Task<List<Store>> LatestAsync(int count);
        /// <summary>پر فروش‌ترین فروشگاه‌های فعال، حداکثر count تا.</summary>
        Task<List<Store>> TopSellingAsync(int count);

        // متدهای مدیریت فروشگاه
        Task<List<Store>> GetByUserIdAsync(int userId);
        Task<Store> GetByIdAsync(int storeId);
        Task<int> CreateAsync(Store store);
        Task<bool> UpdateAsync(Store store);
        Task<(bool CanDelete, List<string> Reasons)> CanDeleteAsync(int storeId);
        Task<bool> DeleteAsync(int storeId);
        Task<bool> ToggleStatusAsync(int storeId, bool status);
        Task<List<Store>> GetAllAsync(); // برای admin
        Task<List<Store>> GetAllForUserAsync(int userId, int? siteId = null);

        /// <summary>
        /// لیست فروشگاه‌ها برای صفحه عمومی با جستجو، فیلتر کوپن/دسته‌بندی و مرتب‌سازی.
        /// sortBy: TopSelling | Latest | ByRating
        /// </summary>
        Task<List<Store>> GetListForPublicAsync(string search, bool onlyWithCoupon, string sortBy, int? categoryId = null, int? cityId = null);
    }
}
