using MyShop.Core.Models.AdminAccess;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Interfaces.AdminAccess
{
    public interface IAdminAccessRepository
    {
        Task<List<AdminAccessProfile>> ListProfilesAsync();
        Task<AdminAccessProfile> GetProfileAsync(int id);
        Task<int> UpsertProfileAsync(AdminAccessProfile profile);
        Task DeleteProfileAsync(int id);
        Task<List<AdminModulePermission>> ListPermissionsAsync(int profileId);
        Task ReplacePermissionsAsync(int profileId, IEnumerable<AdminModulePermission> permissions);
        Task<UserAdminProfileAssignment> GetUserAssignmentAsync(int userId);
        Task AssignUserProfileAsync(int userId, int profileId);
        Task<AdminEffectivePermissions> GetEffectiveAsync(int userId);
        Task<List<AdminAccessOverview>> ListAdminOverviewAsync();
    }

    public interface IAdminPermissionService
    {
        Task<AdminEffectivePermissions> GetEffectiveAsync(int userId);
        Task<bool> HasAsync(int userId, string moduleKey, string action);
        Task<List<AdminAccessProfile>> ListProfilesAsync();
        Task<AdminAccessProfile> GetProfileAsync(int id);
        Task<int> UpsertProfileAsync(AdminAccessProfile profile, IEnumerable<AdminModulePermission> permissions);
        Task DeleteProfileAsync(int id);
        Task AssignUserProfileAsync(int userId, int profileId);
        Task<UserAdminProfileAssignment> GetUserAssignmentAsync(int userId);
        Task<List<AdminAccessOverview>> ListAdminOverviewAsync();
        void InvalidateCache(int userId);
    }
}
