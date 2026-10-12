using System.Collections.Generic;

namespace MyShop.Web.Services
{
    /// <summary>سرویس خواندن و ذخیره وضعیت ماژول‌ها (فعال/غیرفعال) و ترتیب نمایش در منوی ادمین.</summary>
    public interface IModuleSettingsService
    {
        /// <summary>وضعیت فعلی هر ماژول را از تنظیمات برمی‌گرداند.</summary>
        IReadOnlyDictionary<string, bool> GetModules();

        /// <summary>عنوان نمایشی ماژول‌ها به فارسی.</summary>
        IReadOnlyDictionary<string, string> GetModuleDisplayNames();

        /// <summary>ذخیره وضعیت ماژول‌ها در فایل. برای اعمال، برنامه باید ریستارت شود.</summary>
        void SaveModules(IReadOnlyDictionary<string, bool> values);

        /// <summary>ترتیب نمایش ماژول‌ها در منوی ادمین (لیست کلید ماژول).</summary>
        IReadOnlyList<string> GetAdminMenuOrder();

        /// <summary>ذخیره ترتیب منوی ادمین.</summary>
        void SaveAdminMenuOrder(IReadOnlyList<string> orderedModuleKeys);
    }
}
