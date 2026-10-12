using MyShop.Core.Models.Finance;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Interfaces.Finance
{
    public interface IFinanceAccountService
    {
        Task<FinanceAccountDto> GetByIdAsync(int id);
        Task<List<FinanceAccountDto>> GetAllAsync(int? storeId = null, int? companyId = null);
        Task<int> CreateAsync(CreateFinanceAccountDto dto);
        Task<bool> UpdateAsync(UpdateFinanceAccountDto dto);
        Task<bool> DeleteAsync(int id);
    }
}
