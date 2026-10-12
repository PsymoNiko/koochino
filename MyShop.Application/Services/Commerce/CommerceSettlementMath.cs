using System;
using System.Collections.Generic;
using System.Linq;
using MyShop.Core.Models.Commerce;

namespace MyShop.Application.Services.Commerce
{
    public static class CommerceSettlementMath
    {
        public static decimal ResolveRate(IEnumerable<CommerceCommissionRate> rates, int storeId, int? siteId)
        {
            var list = (rates ?? Enumerable.Empty<CommerceCommissionRate>()).Where(r => r != null).ToList();
            var store = list.FirstOrDefault(r => r.StoreId == storeId);
            if (store != null) return ClampRate(store.RatePercent);
            if (siteId.HasValue)
            {
                var site = list.FirstOrDefault(r => !r.StoreId.HasValue && r.SiteId == siteId.Value);
                if (site != null) return ClampRate(site.RatePercent);
            }
            var global = list.FirstOrDefault(r => !r.StoreId.HasValue && !r.SiteId.HasValue);
            return global == null ? 0m : ClampRate(global.RatePercent);
        }

        public static long Commission(long grossAmount, decimal ratePercent)
        {
            if (grossAmount <= 0) return 0;
            var rate = ClampRate(ratePercent);
            var value = (long)Math.Round((double)grossAmount * (double)rate / 100.0, MidpointRounding.AwayFromZero);
            if (value < 0) return 0;
            if (value > grossAmount) return grossAmount;
            return value;
        }

        public static long Net(long grossAmount, long commissionAmount)
        {
            var n = grossAmount - commissionAmount;
            return n < 0 ? 0 : n;
        }

        public static decimal ClampRate(decimal ratePercent)
        {
            if (ratePercent < 0) return 0;
            if (ratePercent > 100) return 100;
            return ratePercent;
        }
    }
}
