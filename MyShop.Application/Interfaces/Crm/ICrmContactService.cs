using MyShop.Core.Models.Crm;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Interfaces.Crm
{
    public interface ICrmContactService
    {
        Task<CrmContactDto> GetByIdAsync(int id);
        Task<CrmContactDto> FindByUserIdAsync(int userId);
        Task<CrmContactDto> FindByPhoneAsync(string phone);
        Task<List<CrmContactDto>> GetAllAsync(int? storeId = null, int? companyId = null, bool? isActive = null);
        Task<List<CrmContactDto>> GetAllForUserAsync(int userId, int? storeId = null, int? companyId = null, bool? isActive = null, int? siteId = null);
        Task<int> CreateAsync(CreateCrmContactDto dto, int? createdBy);
        Task<int> UpsertAsync(CreateCrmContactDto dto, int? createdBy);
        Task<bool> LinkUserAsync(int contactId, int userId);
        Task<bool> UpdateAsync(UpdateCrmContactDto dto);
        Task<bool> DeleteAsync(int id);
    }
}
