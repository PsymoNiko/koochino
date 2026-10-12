using System;
using System.Collections.Generic;
using System.Linq;

namespace MyShop.Core.Models.Modules
{
    public static class ModuleBundleDefs
    {
        /// <summary>وابستگی‌های ثابت (fallback اگر DB خالی باشد).</summary>
        public static readonly IReadOnlyDictionary<string, string[]> DefaultDependsOn =
            new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                ["Commerce"] = new[] { "Products" }
            };

        public static IEnumerable<string> ParseCsv(string csv)
        {
            if (string.IsNullOrWhiteSpace(csv)) yield break;
            foreach (var p in csv.Split(new[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var t = p.Trim();
                if (t.Length > 0) yield return t;
            }
        }
    }
}
