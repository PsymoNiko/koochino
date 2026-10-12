using MyShop.Core.Models.Contracts;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Interfaces.Contracts
{
    public interface IContractService
    {
        Task<ContractDto> GetByIdAsync(int id);
        Task<List<ContractDto>> GetAllAsync(int? storeId = null, int? companyId = null);
        Task<int> CreateAsync(CreateContractDto dto, int? createdBy);
        Task<bool> UpdateAsync(UpdateContractDto dto);
        Task<bool> DeleteAsync(int id);
    }
}
