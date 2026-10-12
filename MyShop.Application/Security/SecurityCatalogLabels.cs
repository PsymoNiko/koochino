using System.Collections.Generic;
using MyShop.Core.Models.Security;

namespace MyShop.Application.Security
{
    /// <summary>
    /// Canonical Persian labels for the security catalog. Used when DB seed text was
    /// corrupted by running migrations with a non-UTF-8 code page.
    /// </summary>
    public static class SecurityCatalogLabels
    {
        static readonly Dictionary<string, string> Actions = new()
        {
            ["View"] = "مشاهده",
            ["Create"] = "ایجاد",
            ["Edit"] = "ویرایش",
            ["Delete"] = "حذف",
            ["Approve"] = "تأیید",
            ["Reject"] = "رد",
            ["Print"] = "چاپ",
            ["Export"] = "خروجی",
            ["Import"] = "ورودی",
            ["Archive"] = "بایگانی",
            ["Restore"] = "بازیابی",
            ["Access"] = "دسترسی"
        };

        static readonly Dictionary<string, string> Modules = new()
        {
            ["Users"] = "کاربران",
            ["Stores"] = "فروشگاه‌ها",
            ["Blog"] = "وبلاگ",
            ["Shop"] = "فروشگاه",
            ["Points"] = "امتیاز",
            ["Announcements"] = "اعلانات",
            ["Common"] = "عمومی",
            ["Companies"] = "شرکت‌ها",
            ["Sliders"] = "اسلایدر",
            ["Products"] = "محصولات",
            ["Personnel"] = "پرسنل",
            ["ProjectManagement"] = "مدیریت پروژه",
            ["Inventory"] = "انبار",
            ["CRM"] = "CRM",
            ["Tickets"] = "تیکت",
            ["Reports"] = "گزارش",
            ["Finance"] = "مالی",
            ["Contracts"] = "قرارداد",
            ["Assets"] = "دارایی",
            ["Training"] = "آموزش",
            ["Academy"] = "آموزشگاه",
            ["Commerce"] = "فروشگاه آنلاین",
            ["AiAssistant"] = "دستیار هوشمند",
            ["Support"] = "پشتیبانی زنده",
            ["Contact"] = "ارتباط",
            ["Sms"] = "پیامک",
            ["SocialMedia"] = "شبکه اجتماعی",
            ["LuckyLeague"] = "جعبه شانس لیگی",
            ["VendorPackages"] = "پکیج فروشنده",
            ["StoreOwnerPortal"] = "پورتال فروشگاه‌دار",
            ["VendorPortal"] = "پورتال فروشنده",
            ["CustomerPortal"] = "پورتال مشتری",
            ["StudentPortal"] = "پورتال دانش‌آموز",
            ["InstructorPortal"] = "پورتال مدرس"
        };

        static readonly Dictionary<string, string> DataScopes = new()
        {
            ["All"] = "همه",
            ["Own"] = "فقط خود",
            ["Site"] = "سایت",
            ["Company"] = "شرکت",
            ["Branch"] = "شعبه",
            ["Department"] = "دپارتمان",
            ["Custom"] = "سفارشی"
        };

        static readonly Dictionary<string, string> PortalFeatures = new()
        {
            ["Dashboard"] = "پیشخوان",
            ["Orders"] = "سفارشات",
            ["Points"] = "امتیاز",
            ["Tickets"] = "تیکت",
            ["Wallet"] = "کیف پول",
            ["Commerce"] = "فروشگاه آنلاین",
            ["Coupons"] = "کوپن",
            ["LuckyLeague"] = "جعبه شانس",
            ["Profile"] = "پروفایل",
            ["Courses"] = "دوره‌ها",
            ["Enrollments"] = "ثبت‌نام",
            ["Sessions"] = "جلسات",
            ["Payments"] = "پرداخت",
            ["Certificates"] = "گواهی",
            ["Evaluations"] = "ارزیابی",
            ["Requests"] = "درخواست",
            ["Reports"] = "گزارش",
            ["ChanceBox"] = "جعبه شانس",
            ["Documents"] = "اسناد",
            ["Modules"] = "ماژول‌ها"
        };

        public static string Action(string actionCode, string fallback = null)
            => Try(Actions, actionCode, fallback);

        public static string Module(string moduleCode, string fallback = null)
            => Try(Modules, moduleCode, fallback);

        public static string DataScope(string scopeCode, string fallback = null)
            => Try(DataScopes, scopeCode, fallback);

        public static string Resource(string resourceCode, string moduleCode, string fallback = null)
        {
            if (string.IsNullOrWhiteSpace(resourceCode))
                return fallback;

            if (resourceCode == "Vendor.Modules")
                return "مدیریت ماژول‌های فروشنده";

            var parts = resourceCode.Split('.');
            if (parts.Length == 3 && parts[2] == "Access" && PortalFeatures.TryGetValue(parts[1], out var feature))
                return "دسترسی " + feature;

            if (parts.Length == 1 && Modules.TryGetValue(parts[0], out var module))
                return module;

            return fallback ?? resourceCode;
        }

        public static void Apply(PermissionCatalogRow row)
        {
            if (row == null) return;
            row.ModuleName = Module(row.ModuleCode, row.ModuleName);
            row.ActionName = Action(row.ActionCode, row.ActionName);
        }

        public static void Apply(RolePermissionMatrixRow row)
        {
            if (row == null) return;
            row.ModuleName = Module(row.ModuleCode, row.ModuleName);
            row.ActionName = Action(row.ActionCode, row.ActionName);
            row.ResourceName = Resource(row.ResourceCode, row.ModuleCode, row.ResourceName);
        }

        public static void Apply(DataScopeOptionRow row)
        {
            if (row == null) return;
            row.Name = DataScope(row.Code, row.Name);
        }

        static string Try(IReadOnlyDictionary<string, string> map, string code, string fallback)
        {
            if (!string.IsNullOrWhiteSpace(code) && map.TryGetValue(code, out var label))
                return label;
            return fallback ?? code;
        }
    }
}
