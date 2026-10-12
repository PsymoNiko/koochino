using MyShop.Core.Models.Companies;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Interfaces.Companies
{
    public interface IHoldingService
    {
        Task<HoldingDto> GetByIdAsync(int id);
        Task<List<HoldingDto>> GetAllAsync(bool? isActive = null);
        Task<int> CreateAsync(CreateHoldingDto dto);
        Task<bool> UpdateAsync(UpdateHoldingDto dto);
        Task<bool> DeleteAsync(int id);
    }
}
