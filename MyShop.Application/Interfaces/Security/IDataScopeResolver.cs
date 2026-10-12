using MyShop.Core.Models.Security;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Interfaces.Security
{
    public interface IUserCompanyRepository
    {
        Task<List<UserCompanyRow>> GetByUserAsync(int userId);
        Task AddAsync(int userId, int companyId);
        Task RemoveAsync(int userId, int companyId);
    }

    public interface IDataScopeResolver
    {
        Task<ScopeDecision> ResolveAsync(int userId, string permissionCode, int? siteId = null);
        Task<DataScopeQuery> BuildQueryAsync(int userId, string permissionCode, int? siteId = null);
    }
}
