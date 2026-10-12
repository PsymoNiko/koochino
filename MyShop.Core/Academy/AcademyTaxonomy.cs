using System;
using System.Collections.Generic;
using System.Linq;
using MyShop.Core.Models.Academy;

namespace MyShop.Core.Academy
{
    public static class AcademyTaxonomy
    {
        public static bool Matches(AcademyActivityCategoryDto category, string code)
        {
            if (category == null || string.IsNullOrWhiteSpace(code)) return false;
            if (!string.IsNullOrWhiteSpace(category.Code)
                && string.Equals(category.Code, code, StringComparison.OrdinalIgnoreCase))
                return true;

            return code switch
            {
                AcademyConstants.Taxonomy.Education => NameIs(category, "آموزش", "Education"),
                AcademyConstants.Taxonomy.Sport => NameIs(category, "ورزش", "Sport"),
                AcademyConstants.Taxonomy.Culture => NameIs(category, "فرهنگ", "Culture"),
                AcademyConstants.Taxonomy.Ceremony => NameIs(category, "مراسم", "Ceremony"),
                _ => false
            };
        }

        public static HashSet<int> CategoryIds(IEnumerable<AcademyActivityCategoryDto> categories, string code)
        {
            var list = (categories ?? Array.Empty<AcademyActivityCategoryDto>()).ToList();
            var ids = new HashSet<int>();
            foreach (var category in list.Where(x => Matches(x, code)))
                CollectDescendants(list, category.Id, ids);
            return ids;
        }

        private static void CollectDescendants(List<AcademyActivityCategoryDto> all, int id, HashSet<int> ids)
        {
            if (!ids.Add(id)) return;
            foreach (var child in all.Where(x => x.ParentId == id))
                CollectDescendants(all, child.Id, ids);
            var node = all.FirstOrDefault(x => x.Id == id);
            foreach (var child in node?.Children ?? new List<AcademyActivityCategoryDto>())
                CollectDescendants(all, child.Id, ids);
        }

        private static bool NameIs(AcademyActivityCategoryDto category, params string[] names) =>
            names.Any(name => string.Equals(category.Name, name, StringComparison.OrdinalIgnoreCase));
    }
}
