using MyShop.Core.Models.Personnel;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Interfaces.Personnel
{
    public interface IPersonnelService
    {
        Task<PersonnelDto> GetByIdAsync(int id);
        Task<List<PersonnelDto>> GetAllAsync(int? storeId = null, int? companyId = null, bool? isActive = null);
        Task<int> CreateAsync(CreatePersonnelDto dto);
        Task<bool> UpdateAsync(UpdatePersonnelDto dto);
        Task<bool> DeleteAsync(int id);
    }
}
