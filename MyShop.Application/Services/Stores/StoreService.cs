using MyShop.Application.Interfaces;
using MyShop.Application.Interfaces.Security;
using MyShop.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Application.Services
{
    public class StoreService : IStoreService
    {
        private readonly IStoreRepository _repo;
        private readonly ICouponRepository _couponRepo;
        private readonly IDataScopeResolver _scope;

        public StoreService(IStoreRepository repo, ICouponRepository couponRepo, IDataScopeResolver scope = null)
        {
            _repo = repo;
            _couponRepo = couponRepo;
            _scope = scope;
        }

        public Task<int> CountAsync()
        {
            return _repo.CountAsync();
        }

        public async Task<List<Store>> LatestAsync(int count)
        {
            var data = await _repo.LatestAsync(count);
            return data.ToList();
        }

        public async Task<List<Store>> TopSellingAsync(int count)
        {
            var data = await _repo.TopSellingAsync(count);
            return data.ToList();
        }

        public Task<List<Store>> GetByUserIdAsync(int userId)
        {
            return _repo.GetByUserIdAsync(userId);
        }

        public Task<Store> GetByIdAsync(int storeId)
        {
            return _repo.GetByIdAsync(storeId);
        }

        public Task<int> CreateAsync(Store store)
        {
            return _repo.CreateAsync(store);
        }

        public Task<bool> UpdateAsync(Store store)
        {
            return _repo.UpdateAsync(store);
        }

        public Task<(bool CanDelete, List<string> Reasons)> CanDeleteAsync(int storeId)
            => _repo.GetDeleteBlockersAsync(storeId);

        public async Task<bool> DeleteAsync(int storeId)
        {
            var (can, reasons) = await _repo.GetDeleteBlockersAsync(storeId);
            if (!can)
                throw new InvalidOperationException(
                    "امکان حذف نیست — با پشتیبانی صحبت کنید. وابستگی‌ها: " + string.Join("، ", reasons));
            return await _repo.DeleteAsync(storeId);
        }

        public Task<bool> ToggleStatusAsync(int storeId, bool status)
        {
            return _repo.ToggleStatusAsync(storeId, status);
        }

        public Task<List<Store>> GetAllAsync()
        {
            return _repo.GetAllAsync();
        }

        public async Task<List<Store>> GetAllForUserAsync(int userId, int? siteId = null)
        {
            if (_scope == null || userId <= 0)
                return await _repo.GetAllAsync();
            var query = await _scope.BuildQueryAsync(userId, "Stores.View", siteId);
            return await _repo.GetAllScopedAsync(query);
        }

        public async Task<List<Store>> GetListForPublicAsync(string search, bool onlyWithCoupon, string sortBy, int? categoryId = null, int? cityId = null)
        {
            var searchTrim = (search ?? "").Trim();
            const int maxCount = 500;

            List<Store> list;
            switch (sortBy)
            {
                case "TopSelling":
                    list = await _repo.TopSellingAsync(maxCount);
                    break;
                case "Latest":
                    list = await _repo.LatestAsync(maxCount);
                    break;
                case "ByRating":
                default:
                    list = await _repo.GetAllAsync();
                    break;
            }

            list = list.Where(s => s.StoreStat).ToList();

            if (categoryId.HasValue && categoryId.Value > 0)
            {
                list = list.Where(s => s.CategoryId == categoryId.Value).ToList();
            }

            if (cityId.HasValue && cityId.Value > 0)
            {
                list = list.Where(s => s.StoreCity == cityId.Value).ToList();
            }

            if (onlyWithCoupon)
            {
                var storeIdsWithCoupon = await _couponRepo.GetStoreIdsHavingCouponAsync();
                var set = new HashSet<int>(storeIdsWithCoupon);
                list = list.Where(s => set.Contains(s.StoreID)).ToList();
            }

            if (!string.IsNullOrEmpty(searchTrim))
            {
                list = list.Where(s =>
                    (s.StoreName ?? "").Contains(searchTrim, System.StringComparison.OrdinalIgnoreCase)
                    || (s.CategoryName ?? "").Contains(searchTrim, System.StringComparison.OrdinalIgnoreCase)
                    || (s.StoreAddress ?? "").Contains(searchTrim, System.StringComparison.OrdinalIgnoreCase)
                ).ToList();
            }

            if (sortBy == "ByRating")
            {
                list = list.OrderByDescending(s => s.StoreRating ?? 0).ToList();
            }

            return list;
        }
    }
}
