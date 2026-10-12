using System.Collections.Generic;
using System.Threading.Tasks;
using MyShop.Core.Models;

namespace MyShop.Application.Interfaces
{
    public interface IPageSeoService
    {
        Task<PageSeo?> GetByPageKeyAsync(string pageKey);
        Task<List<PageSeo>> GetAllAsync();
        Task<bool> SaveAsync(PageSeo entity);
    }
}
