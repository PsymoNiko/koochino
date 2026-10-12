using MyShop.Core.Models.Inventory;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Interfaces.Inventory
{
    public interface IWarehouseService
    {
        Task<WarehouseDto> GetByIdAsync(int id);
        Task<List<WarehouseDto>> GetAllAsync(int? storeId = null, int? companyId = null, bool? isActive = null);
        Task<int> CreateAsync(CreateWarehouseDto dto);
        Task<bool> UpdateAsync(UpdateWarehouseDto dto);
        Task<bool> DeleteAsync(int id);
    }
}
