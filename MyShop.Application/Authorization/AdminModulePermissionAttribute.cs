using Microsoft.AspNetCore.Authorization;

namespace MyShop.Application.Authorization
{
    /// <summary>Maps legacy admin module + CRUD action to permission code for Blazor pages.</summary>
    [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class AdminModulePermissionAttribute : AuthorizeAttribute
    {
        public AdminModulePermissionAttribute(string moduleKey, string action = "Read")
        {
            ModuleKey = moduleKey;
            Action = action;
            Policy = "adminmod:" + moduleKey + ":" + action;
        }

        public string ModuleKey { get; }
        public string Action { get; }
    }
}
