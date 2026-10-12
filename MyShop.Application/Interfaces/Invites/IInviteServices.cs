using MyShop.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Interfaces
{
    public interface IInviteService
    {
        Task<InviteStats> GetInviteStatsAsync(int userId, string baseUrl);
        Task<string> EnsureInviteCodeAsync(int userId);
    }

    public interface IInviteRepository
    {
        Task<string> EnsureInviteCodeAsync(int userId);
        Task<InviteStats> GetStatsAsync(int userId);
    }

    public interface IStoreCategoryService
    {
        Task<List<StoreCategory>> GetAllAsync(bool activeOnly = false);
        Task<int> CreateAsync(StoreCategory category);
        Task<bool> UpdateAsync(StoreCategory category);
        Task<(bool ok, string error)> DeleteAsync(int id);
    }

    public interface IStoreCategoryRepository
    {
        Task<List<StoreCategory>> GetAllAsync(bool activeOnly = false);
        Task<int> CreateAsync(StoreCategory category);
        Task<bool> UpdateAsync(StoreCategory category);
        Task<int> DeleteAsync(int id);
    }
}
