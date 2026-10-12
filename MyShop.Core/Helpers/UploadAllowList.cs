using System;
using System.Collections.Generic;
using System.Linq;

namespace MyShop.Core.Helpers
{
    /// <summary>لیست سخت پسوندهای مجاز آپلود — تنظیمات ادمین نمی‌تواند .exe و مشابه را باز کند.</summary>
    public static class UploadAllowList
    {
        public static readonly HashSet<string> Images = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".gif", ".webp"
        };

        public static readonly HashSet<string> ImagesAndPdf = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".gif", ".webp", ".pdf"
        };

        public static readonly HashSet<string> Documents = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".gif", ".webp", ".pdf", ".doc", ".docx", ".zip"
        };

        public static bool IsAllowed(string ext, ISet<string> set)
        {
            ext = Normalize(ext);
            return !string.IsNullOrEmpty(ext) && set.Contains(ext);
        }

        public static HashSet<string> IntersectWithHardList(string csv, ISet<string> hard)
        {
            if (string.IsNullOrWhiteSpace(csv))
                return new HashSet<string>(hard, StringComparer.OrdinalIgnoreCase);

            var parts = csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(Normalize)
                .Where(x => x.Length > 0 && hard.Contains(x));
            var set = new HashSet<string>(parts, StringComparer.OrdinalIgnoreCase);
            return set.Count == 0
                ? new HashSet<string>(hard, StringComparer.OrdinalIgnoreCase)
                : set;
        }

        public static string Normalize(string ext)
        {
            if (string.IsNullOrWhiteSpace(ext)) return "";
            ext = ext.Trim().ToLowerInvariant();
            return ext.StartsWith('.') ? ext : "." + ext;
        }
    }
}
