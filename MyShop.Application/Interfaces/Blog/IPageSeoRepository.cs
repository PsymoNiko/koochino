using System.Collections.Generic;
using System.Threading.Tasks;
using MyShop.Core.Models;

namespace MyShop.Application.Interfaces
{
    public interface IPageSeoRepository
    {
        Task<PageSeo?> GetByPageKeyAsync(string pageKey);
        Task<List<PageSeo>> GetAllAsync();
        Task<int> UpsertAsync(PageSeo entity);
    }
}
