using Microsoft.AspNetCore.Authorization;
using MyShop.Application.Interfaces.Security;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MyShop.Application.Authorization
{
    public sealed class PermissionRequirement : IAuthorizationRequirement
    {
        public PermissionRequirement(params string[] permissionCodes)
        {
            PermissionCodes = permissionCodes ?? System.Array.Empty<string>();
        }

        public string[] PermissionCodes { get; }
        public bool RequireAll { get; init; }
    }

    public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
    {
        private readonly IPermissionService _permissions;

        public PermissionAuthorizationHandler(IPermissionService permissions)
        {
            _permissions = permissions;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            PermissionRequirement requirement)
        {
            if (requirement.PermissionCodes == null || requirement.PermissionCodes.Length == 0)
            {
                context.Fail();
                return;
            }

            var idStr = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(idStr, out var userId) || userId <= 0)
                return;

            int? siteId = null;
            var siteClaim = context.User?.FindFirst("site_id")?.Value;
            if (int.TryParse(siteClaim, out var s)) siteId = s;

            bool ok;
            if (requirement.RequireAll)
                ok = await _permissions.HasAllPermissionsAsync(userId, requirement.PermissionCodes, siteId);
            else
                ok = await _permissions.HasAnyPermissionAsync(userId, requirement.PermissionCodes, siteId);

            if (ok)
                context.Succeed(requirement);
        }
    }

    public class PermissionPolicyProvider : IAuthorizationPolicyProvider
    {
        private readonly DefaultAuthorizationPolicyProvider _fallback;

        public PermissionPolicyProvider(Microsoft.Extensions.Options.IOptions<AuthorizationOptions> options)
        {
            _fallback = new DefaultAuthorizationPolicyProvider(options);
        }

        public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _fallback.GetDefaultPolicyAsync();

        public Task<AuthorizationPolicy> GetFallbackPolicyAsync() => _fallback.GetFallbackPolicyAsync();

        public Task<AuthorizationPolicy> GetPolicyAsync(string policyName)
        {
            if (!string.IsNullOrWhiteSpace(policyName) && policyName.StartsWith("adminmod:", System.StringComparison.OrdinalIgnoreCase))
            {
                var parts = policyName.Substring(9).Split(':', 2);
                if (parts.Length == 2)
                {
                    var policy = new AuthorizationPolicyBuilder()
                        .AddRequirements(new AdminModulePermissionRequirement(parts[0], parts[1]))
                        .Build();
                    return Task.FromResult(policy);
                }
            }

            if (!string.IsNullOrWhiteSpace(policyName) && policyName.StartsWith("perm:", System.StringComparison.OrdinalIgnoreCase))
            {
                var body = policyName.Substring(5);
                var requireAll = false;
                if (body.StartsWith("all:", System.StringComparison.OrdinalIgnoreCase))
                {
                    requireAll = true;
                    body = body.Substring(4);
                }
                var codes = body.Split('|', System.StringSplitOptions.RemoveEmptyEntries | System.StringSplitOptions.TrimEntries);
                var policy = new AuthorizationPolicyBuilder()
                    .AddRequirements(new PermissionRequirement(codes) { RequireAll = requireAll })
                    .Build();
                return Task.FromResult(policy);
            }
            return _fallback.GetPolicyAsync(policyName);
        }
    }
}
