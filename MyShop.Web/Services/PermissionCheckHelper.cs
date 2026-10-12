using MyShop.Application.Interfaces.AdminAccess;
using MyShop.Core.Models.AdminAccess;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MyShop.Web.Services
{
    public static class PermissionCheckHelper
    {
        public static async Task<bool> HasAdminModuleAsync(
            ClaimsPrincipal user,
            IAdminPermissionService perms,
            string moduleKey,
            string action = null)
        {
            if (user?.Identity?.IsAuthenticated != true || perms == null)
                return false;
            var idStr = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(idStr, out var userId) || userId <= 0)
                return false;
            return await perms.HasAsync(userId, moduleKey, action ?? AdminPermissionActions.Read);
        }
    }
}
