using System.Collections.Generic;
using System.Linq;
using MyShop.Core.Models.Commerce;

namespace MyShop.Application.Services.Commerce
{
    /// <summary>
    /// Line-level store attribution for Commerce reports.
    /// Header Orders.StoreId is only a fallback when the line has no StoreId (pre–multi-vendor).
    /// </summary>
    public static class CommerceReportMath
    {
        public static int? AttributeStore(int? lineStoreId, int? headerStoreId)
        {
            if (lineStoreId.HasValue && lineStoreId.Value > 0)
                return lineStoreId.Value;
            if (headerStoreId.HasValue && headerStoreId.Value > 0)
                return headerStoreId.Value;
            return null;
        }

        public static bool CountsAsPaidRevenue(byte paymentStatus, byte fulfillmentStatus)
        {
            if (paymentStatus != CommercePaymentStatus.Paid)
                return false;
            if (fulfillmentStatus == CommerceFulfillmentStatus.Cancelled
                || fulfillmentStatus == CommerceFulfillmentStatus.Refunded)
                return false;
            return true;
        }

        public static List<CommerceStoreSalesRow> Aggregate(IEnumerable<CommerceReportLine> lines)
        {
            var map = new Dictionary<int, Acc>();
            foreach (var line in lines ?? Enumerable.Empty<CommerceReportLine>())
            {
                if (line == null || !CountsAsPaidRevenue(line.PaymentStatus, line.FulfillmentStatus))
                    continue;
                var storeId = AttributeStore(line.LineStoreId, line.HeaderStoreId);
                if (!storeId.HasValue)
                    continue;
                if (!map.TryGetValue(storeId.Value, out var acc))
                {
                    acc = new Acc();
                    map[storeId.Value] = acc;
                }
                acc.Orders.Add(line.OrderId);
                acc.LineCount++;
                acc.LineAmount += line.LineAmount;
            }

            return map
                .Select(kv => new CommerceStoreSalesRow
                {
                    StoreId = kv.Key,
                    OrderCount = kv.Value.Orders.Count,
                    LineCount = kv.Value.LineCount,
                    LineAmount = kv.Value.LineAmount
                })
                .OrderByDescending(r => r.LineAmount)
                .ThenBy(r => r.StoreId)
                .ToList();
        }

        public static List<CommerceStoreSalesRow> FilterOwned(
            IEnumerable<CommerceStoreSalesRow> rows, ISet<int> ownedStoreIds)
        {
            if (ownedStoreIds == null || ownedStoreIds.Count == 0)
                return new List<CommerceStoreSalesRow>();
            return (rows ?? Enumerable.Empty<CommerceStoreSalesRow>())
                .Where(r => r != null && ownedStoreIds.Contains(r.StoreId))
                .ToList();
        }

        sealed class Acc
        {
            public HashSet<int> Orders { get; } = new();
            public int LineCount;
            public long LineAmount;
        }
    }
}
