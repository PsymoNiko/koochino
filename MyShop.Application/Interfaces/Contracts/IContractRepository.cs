using MyShop.Core.Models.Contracts;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Interfaces.Contracts
{
    public interface IContractRepository
    {
        Task<ContractDto> GetByIdAsync(int id);
        Task<List<ContractDto>> GetAllAsync(int? storeId = null, int? companyId = null);
        Task<int> InsertAsync(Contract entity);
        Task<bool> UpdateAsync(Contract entity);
        Task<bool> DeleteAsync(int id);
    }
}
