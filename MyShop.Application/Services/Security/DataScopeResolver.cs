using MyShop.Application.Interfaces.Security;
using MyShop.Core.Models.Security;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Application.Services.Security
{
    public class DataScopeResolver : IDataScopeResolver
    {
        private readonly IPermissionService _permissions;
        private readonly IUserCompanyRepository _userCompanies;

        public DataScopeResolver(IPermissionService permissions, IUserCompanyRepository userCompanies)
        {
            _permissions = permissions;
            _userCompanies = userCompanies;
        }

        public Task<ScopeDecision> ResolveAsync(int userId, string permissionCode, int? siteId = null)
            => _permissions.GetScopeAsync(userId, permissionCode, siteId);

        public async Task<DataScopeQuery> BuildQueryAsync(int userId, string permissionCode, int? siteId = null)
        {
            var decision = await ResolveAsync(userId, permissionCode, siteId);
            if (decision.IsUnrestricted || string.Equals(decision.Kind, "All", System.StringComparison.OrdinalIgnoreCase))
                return DataScopeQuery.Unrestricted();

            if (string.Equals(decision.Kind, "Site", System.StringComparison.OrdinalIgnoreCase) && decision.Values?.Count > 0)
            {
                return new DataScopeQuery
                {
                    Kind = "Site",
                    Values = decision.Values.ToList(),
                    ActorUserId = userId
                };
            }

            if (string.Equals(decision.Kind, "Own", System.StringComparison.OrdinalIgnoreCase))
            {
                return new DataScopeQuery
                {
                    Kind = "Own",
                    Values = new List<int> { userId },
                    ActorUserId = userId
                };
            }

            if (string.Equals(decision.Kind, "Company", System.StringComparison.OrdinalIgnoreCase))
            {
                var companies = decision.Values?.Count > 0
                    ? decision.Values.ToList()
                    : (await _userCompanies.GetByUserAsync(userId)).Select(c => c.CompanyId).Distinct().ToList();
                return new DataScopeQuery
                {
                    Kind = "Company",
                    Values = companies,
                    ActorUserId = userId
                };
            }

            return DataScopeQuery.Unrestricted();
        }
    }
}
