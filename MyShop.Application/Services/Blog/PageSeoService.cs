using System.Collections.Generic;
using System.Threading.Tasks;
using MyShop.Application.Interfaces;
using MyShop.Core.Models;

namespace MyShop.Application.Services
{
    public class PageSeoService : IPageSeoService
    {
        private readonly IPageSeoRepository _repo;

        public PageSeoService(IPageSeoRepository repo)
        {
            _repo = repo;
        }

        public Task<PageSeo?> GetByPageKeyAsync(string pageKey) => _repo.GetByPageKeyAsync(pageKey);
        public Task<List<PageSeo>> GetAllAsync() => _repo.GetAllAsync();

        public async Task<bool> SaveAsync(PageSeo entity)
        {
            if (string.IsNullOrWhiteSpace(entity?.PageKey))
                return false;
            await _repo.UpsertAsync(entity);
            return true;
        }
    }
}
