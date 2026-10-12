using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using MyShop.Application.Interfaces;
using MyShop.Core.Interfaces;
using MyShop.Core.Models.Shop;

namespace MyShop.Infrastructure.Data
{
    public class StoreOwnerSalesReportRepository : IStoreOwnerSalesReportRepository
    {
        const string FilterSql = @"
FROM koch.Orders o
INNER JOIN koch.Stores s ON o.StoreId = s.StoreID
WHERE s.UserId = @StoreOwnerUserId
  AND o.Status = 1
  AND (@StoreId IS NULL OR o.StoreId = @StoreId)
  AND (@CustomerUserId IS NULL OR o.UserId = @CustomerUserId)
  AND (@FromDate IS NULL OR CAST(o.OrderDate AS DATE) >= @FromDate)
  AND (@ToDate IS NULL OR CAST(o.OrderDate AS DATE) <= @ToDate)";

        const string SummarySql = @"
SELECT
    ISNULL(SUM(o.FinalAmount), 0) AS TotalAmount,
    COUNT(*) AS OrderCount,
    COUNT(DISTINCT o.UserId) AS CustomerCount,
    CASE WHEN COUNT(*) = 0 THEN CAST(0 AS BIGINT) ELSE SUM(o.FinalAmount) / COUNT(*) END AS AvgOrderAmount
" + FilterSql;

        const string ByDaySql = @"
SELECT CAST(o.OrderDate AS DATE) AS SaleDate, SUM(o.FinalAmount) AS Amount, COUNT(*) AS OrderCount
" + FilterSql + @"
GROUP BY CAST(o.OrderDate AS DATE)
ORDER BY CAST(o.OrderDate AS DATE)";

        const string ByStoreSql = @"
SELECT
    s.StoreID AS StoreId,
    ISNULL(s.StoreName, N'فروشگاه') AS StoreName,
    ISNULL(SUM(o.FinalAmount), 0) AS Amount,
    COUNT(o.Id) AS OrderCount,
    COUNT(DISTINCT o.UserId) AS CustomerCount
FROM koch.Stores s
LEFT JOIN koch.Orders o
    ON o.StoreId = s.StoreID
   AND o.Status = 1
   AND (@CustomerUserId IS NULL OR o.UserId = @CustomerUserId)
   AND (@FromDate IS NULL OR CAST(o.OrderDate AS DATE) >= @FromDate)
   AND (@ToDate IS NULL OR CAST(o.OrderDate AS DATE) <= @ToDate)
WHERE s.UserId = @StoreOwnerUserId
  AND (@StoreId IS NULL OR s.StoreID = @StoreId)
GROUP BY s.StoreID, s.StoreName
ORDER BY SUM(o.FinalAmount) DESC, s.StoreName";

        const string SearchCustomersSql = @"
SELECT TOP (20)
    u.Id AS UserId,
    LTRIM(RTRIM(ISNULL(u.FirstName, N'') + N' ' + ISNULL(u.LastName, N''))) AS FullName,
    ISNULL(u.PhoneNumber, N'') AS Phone,
    COUNT(*) AS OrderCount,
    SUM(o.FinalAmount) AS TotalAmount
FROM koch.Orders o
INNER JOIN koch.Stores s ON o.StoreId = s.StoreID
INNER JOIN koch.Users u ON o.UserId = u.Id
WHERE s.UserId = @StoreOwnerUserId
  AND o.Status = 1
  AND (@StoreId IS NULL OR o.StoreId = @StoreId)
  AND (
        ISNULL(u.FirstName, N'') + N' ' + ISNULL(u.LastName, N'') LIKE N'%' + @SearchText + N'%'
        OR ISNULL(u.LastName, N'') LIKE N'%' + @SearchText + N'%'
        OR ISNULL(u.PhoneNumber, N'') LIKE N'%' + @SearchText + N'%'
      )
GROUP BY u.Id, u.FirstName, u.LastName, u.PhoneNumber
ORDER BY SUM(o.FinalAmount) DESC, u.Id DESC";

        private readonly IDbConnectionFactory _factory;

        public StoreOwnerSalesReportRepository(IDbConnectionFactory factory)
        {
            _factory = factory;
        }

        public async Task<StoreOwnerSalesSummaryDto> GetSummaryAsync(StoreOwnerSalesFilter filter)
        {
            using var conn = _factory.CreateConnection();
            var p = ToParams(filter);
            try
            {
                return await conn.QueryFirstOrDefaultAsync<StoreOwnerSalesSummaryDto>(
                    "koch.spOrders_StoreOwnerSales_Summary", p, commandType: CommandType.StoredProcedure)
                    ?? new StoreOwnerSalesSummaryDto();
            }
            catch
            {
                return await conn.QueryFirstOrDefaultAsync<StoreOwnerSalesSummaryDto>(SummarySql, p)
                    ?? new StoreOwnerSalesSummaryDto();
            }
        }

        public async Task<List<StoreOwnerSalesDayDto>> GetByDayAsync(StoreOwnerSalesFilter filter)
        {
            using var conn = _factory.CreateConnection();
            var p = ToParams(filter);
            try
            {
                return (await conn.QueryAsync<StoreOwnerSalesDayDto>(
                    "koch.spOrders_StoreOwnerSales_ByDay", p, commandType: CommandType.StoredProcedure)).ToList();
            }
            catch
            {
                return (await conn.QueryAsync<StoreOwnerSalesDayDto>(ByDaySql, p)).ToList();
            }
        }

        public async Task<List<StoreOwnerStoreSalesDto>> GetByStoreAsync(StoreOwnerSalesFilter filter)
        {
            using var conn = _factory.CreateConnection();
            var p = ToParams(filter);
            try
            {
                return (await conn.QueryAsync<StoreOwnerStoreSalesDto>(
                    "koch.spOrders_StoreOwnerSales_ByStore", p, commandType: CommandType.StoredProcedure)).ToList();
            }
            catch
            {
                return (await conn.QueryAsync<StoreOwnerStoreSalesDto>(ByStoreSql, p)).ToList();
            }
        }

        public async Task<List<StoreOwnerSalesCustomerDto>> SearchCustomersAsync(int storeOwnerUserId, int? storeId, string searchText)
        {
            using var conn = _factory.CreateConnection();
            var p = new
            {
                StoreOwnerUserId = storeOwnerUserId,
                StoreId = storeId,
                SearchText = searchText
            };
            try
            {
                return (await conn.QueryAsync<StoreOwnerSalesCustomerDto>(
                    "koch.spOrders_StoreOwnerSales_SearchCustomers", p, commandType: CommandType.StoredProcedure)).ToList();
            }
            catch
            {
                return (await conn.QueryAsync<StoreOwnerSalesCustomerDto>(SearchCustomersSql, p)).ToList();
            }
        }

        static object ToParams(StoreOwnerSalesFilter filter)
        {
            filter ??= new StoreOwnerSalesFilter();
            return new
            {
                filter.StoreOwnerUserId,
                filter.StoreId,
                filter.CustomerUserId,
                FromDate = filter.FromDate?.Date,
                ToDate = filter.ToDate?.Date
            };
        }
    }
}
