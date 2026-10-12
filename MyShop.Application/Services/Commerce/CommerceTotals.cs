using System;
using System.Collections.Generic;
using System.Linq;
using MyShop.Core.Models.Commerce;

namespace MyShop.Application.Services.Commerce
{
    public static class CommerceTotals
    {
        public static long QuoteShippingFee(
            CommerceShippingMethod method,
            long subtotal,
            IReadOnlyList<int> storeIds,
            IReadOnlyList<CommerceStoreShippingRate> storeRates)
        {
            long fee;
            var ids = (storeIds ?? Array.Empty<int>()).Where(id => id > 0).Distinct().ToList();
            var rates = storeRates ?? Array.Empty<CommerceStoreShippingRate>();
            if (ids.Count > 0 && rates.Count > 0)
            {
                fee = 0;
                foreach (var sid in ids)
                {
                    var r = rates.FirstOrDefault(x => x.StoreId == sid);
                    fee += r != null ? r.Fee : (method?.Fee ?? 0);
                }
            }
            else
                fee = method?.Fee ?? 0;

            if (method?.FreeOverAmount.HasValue == true && method.FreeOverAmount.Value > 0 && subtotal >= method.FreeOverAmount.Value)
                return 0;
            return fee < 0 ? 0 : fee;
        }

        public static long QuoteTax(CommerceTaxRate rate, long subtotal, long discount, long shippingFee)
        {
            if (rate == null || !rate.IsActive || rate.RatePercent <= 0) return 0;
            var basis = subtotal - discount;
            if (basis < 0) basis = 0;
            if (rate.AppliesToShipping) basis += shippingFee;
            var tax = (long)Math.Floor(basis * rate.RatePercent / 100m);
            return tax < 0 ? 0 : tax;
        }
    }
}
