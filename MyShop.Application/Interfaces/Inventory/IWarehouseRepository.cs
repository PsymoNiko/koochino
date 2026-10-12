using MyShop.Core.Models.Inventory;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Interfaces.Inventory
{
    public interface IWarehouseRepository
    {
        Task<WarehouseDto> GetByIdAsync(int id);
        Task<List<WarehouseDto>> GetAllAsync(int? storeId = null, int? companyId = null, bool? isActive = null);
        Task<int> InsertAsync(Warehouse entity);
        Task<bool> UpdateAsync(Warehouse entity);
        Task<bool> DeleteAsync(int id);
    }
}
