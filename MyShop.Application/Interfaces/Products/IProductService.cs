using MyShop.Core.Models.Products;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Interfaces.Products
{
    public interface IProductService
    {
        Task<ProductDto> GetByIdAsync(int id);
        Task<List<ProductDto>> GetAllAsync(int? storeId = null, int? companyId = null);
        Task<int> CreateAsync(CreateProductDto dto);
        Task<bool> UpdateAsync(UpdateProductDto dto);
        Task<bool> DeleteAsync(int id);
    }
}
