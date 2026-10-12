using Microsoft.AspNetCore.Authorization;

namespace MyShop.Application.Authorization
{
    [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public sealed class RequirePermissionAttribute : AuthorizeAttribute
    {
        public RequirePermissionAttribute(string permissionCode)
        {
            Policy = BuildPolicy(permissionCode, false);
        }

        public RequirePermissionAttribute(string permissionCode, bool requireAll, params string[] additionalCodes)
        {
            var codes = new System.Collections.Generic.List<string> { permissionCode };
            if (additionalCodes != null)
                codes.AddRange(additionalCodes);
            Policy = requireAll
                ? "perm:all:" + string.Join("|", codes)
                : "perm:" + string.Join("|", codes);
        }

        static string BuildPolicy(string code, bool requireAll)
            => requireAll ? "perm:all:" + code : "perm:" + code;
    }
}
