using MyShop.Core.Models.Products;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Interfaces.Products
{
    public interface IProductRepository
    {
        Task<ProductDto> GetByIdAsync(int id);
        Task<List<ProductDto>> GetAllAsync(int? storeId = null, int? companyId = null);
        Task<int> InsertAsync(Product entity);
        Task<bool> UpdateAsync(Product entity);
        Task<bool> DeleteAsync(int id);
        Task SetUnitIdAsync(int productId, int? unitId);
    }
}
