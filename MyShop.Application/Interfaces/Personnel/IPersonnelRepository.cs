using System.Collections.Generic;
using System.Threading.Tasks;
using MyShop.Core.Models.Personnel;

namespace MyShop.Application.Interfaces.Personnel
{
    public interface IPersonnelRepository
    {
        Task<PersonnelDto> GetByIdAsync(int id, int siteId);
        Task<List<PersonnelDto>> GetAllAsync(int siteId, int? storeId = null, int? companyId = null, bool? isActive = null);
        Task<int> InsertAsync(MyShop.Core.Models.Personnel.Personnel entity);
        Task<bool> UpdateAsync(MyShop.Core.Models.Personnel.Personnel entity);
        Task<bool> DeleteAsync(int id, int siteId);
    }
}
