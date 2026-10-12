using System;
using System.IO;
using MyShop.Core.Helpers;

namespace MyShop.Application.Services.Commerce
{
    internal static class CommerceCatalogValidation
    {
        public static string RequireName(string name, int max)
        {
            var n = (name ?? "").Trim();
            if (n.Length == 0) throw new InvalidOperationException("نام الزامی است.");
            if (n.Length > max) throw new InvalidOperationException("نام بیش از حد طولانی است.");
            return n;
        }

        public static string Slug(string slug, string name)
        {
            var s = (slug ?? "").Trim().ToLowerInvariant();
            if (s.Length == 0)
            {
                s = (name ?? "").Trim().ToLowerInvariant().Replace(" ", "-");
            }
            if (s.Length > 160) s = s.Substring(0, 160);
            return s;
        }

        public static int SortOrder(int sort)
        {
            if (sort < 0) throw new InvalidOperationException("ترتیب نمی‌تواند منفی باشد.");
            return sort;
        }

        public static string ValidateImageUrl(string url)
        {
            var u = (url ?? "").Trim();
            if (u.Length == 0) throw new InvalidOperationException("آدرس تصویر الزامی است.");
            if (u.Length > 1024) throw new InvalidOperationException("آدرس تصویر بیش از حد طولانی است.");
            if (u.Contains("javascript:", StringComparison.OrdinalIgnoreCase) || u.Contains("data:", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("آدرس تصویر نامعتبر است.");
            if (!(u.StartsWith("/", StringComparison.Ordinal) || u.StartsWith("https://", StringComparison.OrdinalIgnoreCase) || u.StartsWith("http://", StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("آدرس تصویر باید نسبی یا http(s) باشد.");
            var path = u;
            var q = path.IndexOf('?');
            if (q >= 0) path = path.Substring(0, q);
            var ext = Path.GetExtension(path);
            if (!UploadAllowList.IsAllowed(ext, UploadAllowList.Images))
                throw new InvalidOperationException("پسوند تصویر مجاز نیست.");
            return u;
        }
    }
}
