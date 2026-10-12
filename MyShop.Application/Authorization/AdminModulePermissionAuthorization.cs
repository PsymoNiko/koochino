using Microsoft.AspNetCore.Authorization;
using MyShop.Application.Interfaces.AdminAccess;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MyShop.Application.Authorization
{
    public sealed class AdminModulePermissionRequirement : IAuthorizationRequirement
    {
        public AdminModulePermissionRequirement(string moduleKey, string action)
        {
            ModuleKey = moduleKey;
            Action = action;
        }

        public string ModuleKey { get; }
        public string Action { get; }
    }

    /// <summary>Evaluates [AdminModulePermission] via IAdminPermissionService (engine + legacy facade).</summary>
    public sealed class AdminModulePermissionAuthorizationHandler
        : AuthorizationHandler<AdminModulePermissionRequirement>
    {
        private readonly IAdminPermissionService _adminPerms;

        public AdminModulePermissionAuthorizationHandler(IAdminPermissionService adminPerms)
        {
            _adminPerms = adminPerms;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            AdminModulePermissionRequirement requirement)
        {
            if (string.IsNullOrWhiteSpace(requirement.ModuleKey))
            {
                context.Fail();
                return;
            }

            var idStr = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(idStr, out var userId) || userId <= 0)
                return;

            if (await _adminPerms.HasAsync(userId, requirement.ModuleKey, requirement.Action))
                context.Succeed(requirement);
        }
    }
}
