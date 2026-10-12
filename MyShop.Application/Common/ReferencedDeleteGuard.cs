using System;
using System.Collections.Generic;
using System.Linq;

namespace MyShop.Application.Common
{
    public static class ReferencedDeleteGuard
    {
        public static void ThrowIfReferenced(IEnumerable<string> reasons)
        {
            var list = (reasons ?? Enumerable.Empty<string>())
                .Where(r => !string.IsNullOrWhiteSpace(r))
                .ToList();
            if (list.Count == 0) return;
            throw new InvalidOperationException(
                "امکان حذف نیست چون این مورد در بخش‌های دیگر استفاده شده است: " + string.Join("، ", list) + ".");
        }
    }
}
