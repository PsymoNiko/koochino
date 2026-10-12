using MyShop.Application.Interfaces;
using MyShop.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Services
{
    public class InviteService : IInviteService
    {
        private readonly IInviteRepository _repo;

        public InviteService(IInviteRepository repo)
        {
            _repo = repo;
        }

        public Task<string> EnsureInviteCodeAsync(int userId)
            => _repo.EnsureInviteCodeAsync(userId);

        public async Task<InviteStats> GetInviteStatsAsync(int userId, string baseUrl)
        {
            var code = await _repo.EnsureInviteCodeAsync(userId);
            var stats = await _repo.GetStatsAsync(userId) ?? new InviteStats();
            stats.InviteCode = code;
            var root = (baseUrl ?? "").TrimEnd('/');
            stats.InviteLink = $"{root}/Account/UserRegister?ref={code}";
            return stats;
        }
    }

    public class StoreCategoryService : IStoreCategoryService
    {
        private readonly IStoreCategoryRepository _repo;

        public StoreCategoryService(IStoreCategoryRepository repo)
        {
            _repo = repo;
        }

        public Task<List<StoreCategory>> GetAllAsync(bool activeOnly = false)
            => _repo.GetAllAsync(activeOnly);

        public Task<int> CreateAsync(StoreCategory category)
            => _repo.CreateAsync(category);

        public Task<bool> UpdateAsync(StoreCategory category)
            => _repo.UpdateAsync(category);

        public async Task<(bool ok, string error)> DeleteAsync(int id)
        {
            var result = await _repo.DeleteAsync(id);
            return result switch
            {
                1 => (true, null),
                -1 => (false, "این دسته‌بندی روی فروشگاه‌ها استفاده شده و قابل حذف نیست."),
                _ => (false, "حذف انجام نشد.")
            };
        }
    }
}
