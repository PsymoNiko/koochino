using MyShop.Core.Models.Companies;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Interfaces.Companies
{
    public interface IHoldingRepository
    {
        Task<HoldingDto> GetByIdAsync(int id);
        Task<List<HoldingDto>> GetAllAsync(bool? isActive = null);
        Task<int> InsertAsync(Holding entity);
        Task<bool> UpdateAsync(Holding entity);
        Task<bool> DeleteAsync(int id);
    }
}
