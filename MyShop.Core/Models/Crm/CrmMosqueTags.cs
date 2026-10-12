using System;
using System.Collections.Generic;
using System.Linq;

namespace MyShop.Core.Models.Crm
{
    public static class CrmMosqueTags
    {
        public const string Parent = "والد";
        public const string Congregation = "جماعت";
        public const string Donor = "خیر";
        public const string Volunteer = "داوطلب";

        public static readonly string[] All = { Parent, Congregation, Donor, Volunteer };

        public static bool Has(string tags, string tag)
        {
            if (string.IsNullOrWhiteSpace(tags) || string.IsNullOrWhiteSpace(tag))
                return false;
            return Split(tags).Contains(tag.Trim(), StringComparer.OrdinalIgnoreCase);
        }

        public static string Set(string tags, string tag, bool on)
        {
            var set = new HashSet<string>(Split(tags), StringComparer.OrdinalIgnoreCase);
            tag = (tag ?? "").Trim();
            if (tag.Length == 0) return string.Join(", ", set);
            if (on) set.Add(tag);
            else set.RemoveWhere(x => string.Equals(x, tag, StringComparison.OrdinalIgnoreCase));
            return string.Join(", ", set);
        }

        public static IEnumerable<string> Split(string tags) =>
            (tags ?? "").Split(new[] { ',', '،', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => x.Length > 0);
    }
}
