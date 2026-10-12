namespace MyShop.Core.Models.Security
{
    /// <summary>کدهای مجوز پورتال‌های غیرادمین — فاز ۸.</summary>
    public static class PortalPermissionCodes
    {
        public static string StoreOwnerModule(string moduleKey) => $"StoreOwner.{moduleKey}.Access";
        public static string CustomerFeature(string featureKey) => $"Customer.{featureKey}.Access";
        public static string InstructorFeature(string featureKey) => $"Instructor.{featureKey}.Access";
        public static string StudentFeature(string featureKey) => $"Student.{featureKey}.Access";
        public static string VendorFeature(string featureKey) => $"Vendor.{featureKey}.Access";

        public const string VendorModulesView = "Vendor.Modules.View";
    }
}
