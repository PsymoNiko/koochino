using System;
using System.Collections.Generic;

namespace MyShop.Core.Models.AdminAccess
{
    public static class AdminPermissionActions
    {
        public const string Read = "Read";
        public const string Create = "Create";
        public const string Update = "Update";
        public const string Delete = "Delete";
    }

    public class AdminAccessProfile
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsSystem { get; set; }
        public bool IsFullAccess { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<AdminModulePermission> Permissions { get; set; } = new();
    }

    public class AdminModulePermission
    {
        public int Id { get; set; }
        public int ProfileId { get; set; }
        public string ModuleKey { get; set; }
        public bool CanRead { get; set; }
        public bool CanCreate { get; set; }
        public bool CanUpdate { get; set; }
        public bool CanDelete { get; set; }
    }

    public class UserAdminProfileAssignment
    {
        public int UserId { get; set; }
        public int ProfileId { get; set; }
        public string ProfileName { get; set; }
        public bool IsFullAccess { get; set; }
        public bool IsSystem { get; set; }
        public DateTime AssignedAt { get; set; }
    }

    public class AdminEffectivePermissions
    {
        public int UserId { get; set; }
        public int ProfileId { get; set; }
        public bool IsFullAccess { get; set; }
        public List<AdminModulePermission> Permissions { get; set; } = new();

        public bool Has(string moduleKey, string action)
        {
            if (IsFullAccess) return true;
            if (string.IsNullOrWhiteSpace(moduleKey)) return false;
            if (ProfileId <= 0 && (Permissions == null || Permissions.Count == 0)) return false;
            var perm = Permissions?.Find(p =>
                string.Equals(p.ModuleKey, moduleKey, StringComparison.OrdinalIgnoreCase));
            if (perm == null) return false;
            return action switch
            {
                AdminPermissionActions.Read => perm.CanRead,
                AdminPermissionActions.Create => perm.CanCreate,
                AdminPermissionActions.Update => perm.CanUpdate,
                AdminPermissionActions.Delete => perm.CanDelete,
                _ => false
            };
        }
    }
}
