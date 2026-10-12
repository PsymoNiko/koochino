using MyShop.Core.Models.AdminAccess;

namespace MyShop.Web.Services
{
    /// <summary>مجوزهای مؤثر ادمین؛ از AdminLayout به صفحات cascade می‌شود.</summary>
    public class AdminAuthState
    {
        public AdminEffectivePermissions Effective { get; set; }
        public bool Loaded { get; set; }

        public bool Can(string moduleKey, string action)
        {
            if (!Loaded || Effective == null)
                return false;
            return Effective.Has(moduleKey, action);
        }

        public bool CanRead(string moduleKey) => Can(moduleKey, AdminPermissionActions.Read);
        public bool CanCreate(string moduleKey) => Can(moduleKey, AdminPermissionActions.Create);
        public bool CanUpdate(string moduleKey) => Can(moduleKey, AdminPermissionActions.Update);
        public bool CanDelete(string moduleKey) => Can(moduleKey, AdminPermissionActions.Delete);
    }
}
