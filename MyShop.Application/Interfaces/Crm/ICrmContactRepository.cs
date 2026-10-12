using MyShop.Core.Models.Crm;
using MyShop.Core.Models.Security;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Interfaces.Crm
{
    public interface ICrmContactRepository
    {
        Task<CrmContactDto> GetByIdAsync(int id, int siteId);
        Task<CrmContactDto> GetByUserIdAsync(int siteId, int userId);
        Task<CrmContactDto> GetByPhoneAsync(int siteId, string normalizedPhone);
        Task<List<CrmContactDto>> GetAllAsync(int siteId, int? storeId = null, int? companyId = null, bool? isActive = null);
        Task<List<CrmContactDto>> GetAllScopedAsync(DataScopeQuery scope, int siteId, int? storeId = null, int? companyId = null, bool? isActive = null)
            => GetAllAsync(siteId, storeId, companyId, isActive);
        Task<bool> UserBelongsToSiteAsync(int userId, int siteId);
        Task<int> InsertAsync(CrmContact entity);
        Task<bool> UpdateAsync(CrmContact entity);
        Task<bool> DeleteAsync(int id, int siteId);
    }
}
