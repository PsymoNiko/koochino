using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using MyShop.Application.Interfaces;
using MyShop.Core.Models;

namespace MyShop.Web.Services
{
    /// <summary>کش در حافظه برای لیست فروشگاه‌های عمومی.</summary>
    public class StoreListCacheService : IStoreListCacheService
    {
        private const string KeyPrefixTop = "StoreList:TopSelling:";
        private const string KeyPrefixPublic = "StoreList:Public:";
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

        private readonly IStoreService _storeService;
        private readonly IMemoryCache _cache;

        public StoreListCacheService(IStoreService storeService, IMemoryCache cache)
        {
            _storeService = storeService;
            _cache = cache;
        }

        public async Task<List<Store>> TopSellingAsync(int count)
        {
            var key = KeyPrefixTop + count;
            return await _cache.GetOrCreateAsync(key, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CacheDuration;
                var list = await _storeService.TopSellingAsync(count);
                return list ?? new List<Store>();
            });
        }

        public async Task<List<Store>> GetListForPublicAsync(string search, bool onlyWithCoupon, string sortBy, int? categoryId = null, int? cityId = null)
        {
            var searchPart = string.IsNullOrWhiteSpace(search) ? "" : search.Trim();
            var catPart = categoryId.HasValue && categoryId.Value > 0 ? categoryId.Value.ToString() : "";
            var cityPart = cityId.HasValue && cityId.Value > 0 ? cityId.Value.ToString() : "";
            var key = KeyPrefixPublic + searchPart + ":" + onlyWithCoupon + ":" + (sortBy ?? "TopSelling") + ":" + catPart + ":" + cityPart;
            return await _cache.GetOrCreateAsync(key, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CacheDuration;
                var list = await _storeService.GetListForPublicAsync(
                    string.IsNullOrWhiteSpace(search) ? null : search.Trim(),
                    onlyWithCoupon,
                    sortBy ?? "TopSelling",
                    categoryId,
                    cityId);
                return list ?? new List<Store>();
            });
        }
    }
}
