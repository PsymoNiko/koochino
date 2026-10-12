using MyShop.Core.Models.Companies;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Interfaces.Companies
{
    public interface ICompanyRepository
    {
        Task<CompanyDto> GetByIdAsync(int id);
        Task<List<CompanyDto>> GetAllAsync(int? holdingId = null, bool? isActive = null);
        Task<int> InsertAsync(Company entity);
        Task<bool> UpdateAsync(Company entity);
        Task<bool> DeleteAsync(int id);
    }
}
