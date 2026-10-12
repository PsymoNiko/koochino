using MyShop.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Web.Services
{
    /// <summary>کش لیست فروشگاه‌های عمومی برای کاهش بار دیتابیس.</summary>
    public interface IStoreListCacheService
    {
        /// <summary>پر فروش‌ترین فروشگاه‌ها (کش ۵ دقیقه).</summary>
        Task<List<Store>> TopSellingAsync(int count);

        /// <summary>لیست فروشگاه‌ها برای صفحه عمومی با جستجو/فیلتر/مرتب‌سازی (کش ۵ دقیقه).</summary>
        Task<List<Store>> GetListForPublicAsync(string search, bool onlyWithCoupon, string sortBy, int? categoryId = null, int? cityId = null);
    }
}
