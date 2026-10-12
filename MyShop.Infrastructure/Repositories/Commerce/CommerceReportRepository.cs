using Dapper;
using MyShop.Application.Interfaces.Commerce;
using MyShop.Core.Interfaces;
using MyShop.Core.Models.Commerce;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Infrastructure.Repositories.Commerce
{
    public class CommerceReportRepository : ICommerceReportRepository
    {
        private readonly IDbConnectionFactory _factory;
        public CommerceReportRepository(IDbConnectionFactory factory) => _factory = factory;

        public async Task<List<CommerceStoreSalesRow>> GetStoreSalesAsync(
            int? siteId, DateTime fromInclusive, DateTime toExclusive, IReadOnlyList<int> storeIds)
        {
            if (storeIds != null && storeIds.Count == 0)
                return new List<CommerceStoreSalesRow>();

            using var conn = _factory.CreateConnection();
            var hasLineStore = await conn.ExecuteScalarAsync<int>(
                "SELECT CASE WHEN COL_LENGTH(N'koch.OrderItems', N'StoreId') IS NULL THEN 0 ELSE 1 END") == 1;
            var hasLineTotal = await conn.ExecuteScalarAsync<int>(
                "SELECT CASE WHEN COL_LENGTH(N'koch.OrderItems', N'LineTotal') IS NULL THEN 0 ELSE 1 END") == 1;
            var hasTotalPrice = await conn.ExecuteScalarAsync<int>(
                "SELECT CASE WHEN COL_LENGTH(N'koch.OrderItems', N'TotalPrice') IS NULL THEN 0 ELSE 1 END") == 1;

            var amountExpr = hasLineTotal
                ? "ISNULL(oi.LineTotal, CAST(oi.Quantity AS BIGINT) * ISNULL(oi.UnitPrice,0))"
                : hasTotalPrice
                    ? "ISNULL(oi.TotalPrice, CAST(oi.Quantity AS BIGINT) * ISNULL(oi.UnitPrice,0))"
                    : "CAST(oi.Quantity AS BIGINT) * ISNULL(oi.UnitPrice,0)";
            var storeExpr = hasLineStore
                ? "COALESCE(NULLIF(oi.StoreId,0), o.StoreId)"
                : "o.StoreId";

            var storeFilter = storeIds != null
                ? $" AND {storeExpr} IN @StoreIds"
                : "";

            var sql = $@"
                SELECT
                    {storeExpr} AS StoreId,
                    MAX(st.StoreName) AS StoreName,
                    COUNT(DISTINCT o.Id) AS OrderCount,
                    COUNT(1) AS LineCount,
                    CAST(ISNULL(SUM({amountExpr}),0) AS BIGINT) AS LineAmount
                FROM koch.Orders o
                INNER JOIN koch.OrderItems oi ON oi.OrderId = o.Id
                LEFT JOIN koch.Stores st ON st.StoreID = {storeExpr}
                WHERE ISNULL(o.IsCommerce,0)=1
                  AND ISNULL(o.PaymentStatus,0)=@Paid
                  AND ISNULL(o.FulfillmentStatus,0) NOT IN (@Cancelled, @Refunded)
                  AND o.OrderDate >= @FromInclusive
                  AND o.OrderDate < @ToExclusive
                  AND (@SiteId IS NULL OR o.SiteId IS NULL OR o.SiteId=@SiteId)
                  AND {storeExpr} IS NOT NULL
                  AND {storeExpr} > 0
                  {storeFilter}
                GROUP BY {storeExpr}
                ORDER BY LineAmount DESC";

            var rows = await conn.QueryAsync<CommerceStoreSalesRow>(sql, new
            {
                SiteId = siteId,
                FromInclusive = fromInclusive,
                ToExclusive = toExclusive,
                StoreIds = storeIds?.ToArray(),
                Paid = CommercePaymentStatus.Paid,
                Cancelled = CommerceFulfillmentStatus.Cancelled,
                Refunded = CommerceFulfillmentStatus.Refunded
            });
            return rows.ToList();
        }
    }
}
