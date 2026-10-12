using MyShop.Core.Models.Finance;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Interfaces.Finance
{
    public interface IFinanceAccountRepository
    {
        Task<FinanceAccountDto> GetByIdAsync(int id);
        Task<List<FinanceAccountDto>> GetAllAsync(int? storeId = null, int? companyId = null);
        Task<int> InsertAsync(FinanceAccount entity);
        Task<bool> UpdateAsync(FinanceAccount entity);
        Task<bool> DeleteAsync(int id);
    }
}
