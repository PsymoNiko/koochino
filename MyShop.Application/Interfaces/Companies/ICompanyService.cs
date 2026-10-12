using MyShop.Core.Models.Companies;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Interfaces.Companies
{
    public interface ICompanyService
    {
        Task<CompanyDto> GetByIdAsync(int id);
        Task<List<CompanyDto>> GetAllAsync(int? holdingId = null, bool? isActive = null);
        Task<int> CreateAsync(CreateCompanyDto dto);
        Task<bool> UpdateAsync(UpdateCompanyDto dto);
        Task<bool> DeleteAsync(int id);
    }
}
