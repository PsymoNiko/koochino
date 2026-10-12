using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MyShop.Web.Pages
{
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public class ModuleDisabledModel : PageModel
    {
        private static readonly System.Collections.Generic.Dictionary<string, string> ModuleNames = new(System.StringComparer.OrdinalIgnoreCase)
        {
            ["Users"] = "کاربران و دسترسی",
            ["Stores"] = "فروشگاه‌ها",
            ["Blog"] = "بلاگ و محتوا",
            ["Shop"] = "فروش",
            ["Points"] = "امتیاز و ماموریت",
            ["Announcements"] = "اعلانات",
            ["Common"] = "مشترک",
            ["Products"] = "محصولات",
            ["Personnel"] = "پرسونل",
            ["ProjectManagement"] = "مدیریت پروژه",
            ["Inventory"] = "انبار و موجودی",
            ["CRM"] = "CRM",
            ["Tickets"] = "تیکت پشتیبانی",
            ["Contact"] = "ارتباط",
            ["Reports"] = "گزارش‌ها",
            ["Finance"] = "حسابداری",
            ["Contracts"] = "قراردادها",
            ["Assets"] = "دارایی‌ها",
            ["Training"] = "آموزش",
            ["Academy"] = "آموزشگاه"
        };

        public string ModuleKey { get; set; }
        public string ModuleDisplayName => !string.IsNullOrEmpty(ModuleKey) && ModuleNames.TryGetValue(ModuleKey, out var name) ? name : ModuleKey ?? "این بخش";

        public void OnGet(string module)
        {
            ModuleKey = module;
        }
    }
}
