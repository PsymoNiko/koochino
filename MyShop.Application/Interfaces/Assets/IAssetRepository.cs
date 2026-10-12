using MyShop.Core.Models.Assets;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Interfaces.Assets
{
    public interface IAssetRepository
    {
        Task<AssetDto> GetByIdAsync(int id);
        Task<List<AssetDto>> GetAllAsync(int? storeId = null, int? companyId = null, int? categoryId = null);
        Task<List<AssetCategoryItemDto>> GetCategoriesAsync();
        Task<int> InsertAsync(Asset entity);
        Task<bool> UpdateAsync(Asset entity);
        Task<bool> DeleteAsync(int id);
    }
}
