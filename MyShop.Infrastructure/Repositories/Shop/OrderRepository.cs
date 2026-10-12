using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using MyShop.Application.Interfaces;
using MyShop.Core.Helpers;
using MyShop.Core.Interfaces;
using MyShop.Core.Models;
using MyShop.Core.Models.Security;
using MyShop.Core.Security;

namespace MyShop.Infrastructure.Data
{
    public class OrderRepository : IOrderRepository
    {
        private readonly IDbConnectionFactory _factory;
        private readonly ICurrentTenant _tenant;

        public OrderRepository(IDbConnectionFactory factory, ICurrentTenant tenant = null)
        {
            _factory = factory;
            _tenant = tenant;
        }

        public async Task<string> GetNextOrderNumberAsync()
        {
            using var conn = _factory.CreateConnection();
            var parameters = new DynamicParameters();
            parameters.Add("OrderNumber", dbType: DbType.String, size: 30, direction: ParameterDirection.Output);
            await conn.ExecuteAsync(
                "koch.spOrders_GetNextOrderNumber",
                parameters,
                commandType: CommandType.StoredProcedure);
            return parameters.Get<string>("OrderNumber") ?? "";
        }

        public async Task<int> InsertAsync(Order order)
        {
            using var conn = _factory.CreateConnection();

            var siteId = order.SiteId ?? SiteScope.CurrentSiteId(_tenant);
            if (!siteId.HasValue && order.StoreId.HasValue)
            {
                try
                {
                    siteId = await conn.ExecuteScalarAsync<int?>(
                        "SELECT TOP 1 SiteId FROM koch.Stores WHERE StoreID = @Id",
                        new { Id = order.StoreId.Value });
                }
                catch { /* column missing */ }
            }

            var parameters = new DynamicParameters();
            parameters.Add("OrderNumber", order.OrderNumber);
            parameters.Add("UserId", order.UserId);
            parameters.Add("CustomerId", order.UserId);
            parameters.Add("StoreId", order.StoreId);
            parameters.Add("OrderType", order.OrderType);
            parameters.Add("SaleChannel", order.SaleChannel);
            parameters.Add("TotalAmount", order.TotalAmount);
            parameters.Add("DiscountAmount", order.DiscountAmount);
            parameters.Add("FinalAmount", order.FinalAmount);
            parameters.Add("CouponUsageId", order.CouponUsageId);
            parameters.Add("ChanceBoxCodeUsageId", order.ChanceBoxCodeUsageId);
            parameters.Add("Status", order.Status);
            parameters.Add("Notes", order.Notes);
            parameters.Add("SiteId", siteId);
            parameters.Add("OrderId", dbType: DbType.Int32, direction: ParameterDirection.Output);

            try
            {
                await conn.ExecuteAsync(
                    "koch.spOrders_Insert",
                    parameters,
                    commandType: CommandType.StoredProcedure);
            }
            catch
            {
                // SP قدیمی بدون @SiteId
                parameters = new DynamicParameters();
                parameters.Add("OrderNumber", order.OrderNumber);
                parameters.Add("UserId", order.UserId);
                parameters.Add("CustomerId", order.UserId);
                parameters.Add("StoreId", order.StoreId);
                parameters.Add("OrderType", order.OrderType);
                parameters.Add("SaleChannel", order.SaleChannel);
                parameters.Add("TotalAmount", order.TotalAmount);
                parameters.Add("DiscountAmount", order.DiscountAmount);
                parameters.Add("FinalAmount", order.FinalAmount);
                parameters.Add("CouponUsageId", order.CouponUsageId);
                parameters.Add("ChanceBoxCodeUsageId", order.ChanceBoxCodeUsageId);
                parameters.Add("Status", order.Status);
                parameters.Add("Notes", order.Notes);
                parameters.Add("OrderId", dbType: DbType.Int32, direction: ParameterDirection.Output);
                await conn.ExecuteAsync(
                    "koch.spOrders_Insert",
                    parameters,
                    commandType: CommandType.StoredProcedure);
            }

            var orderId = parameters.Get<int>("OrderId");
            if (orderId > 0 && siteId.HasValue && siteId.Value > 0)
            {
                try
                {
                    await conn.ExecuteAsync(
                        "UPDATE koch.Orders SET SiteId = @SiteId WHERE Id = @OrderId AND (SiteId IS NULL OR SiteId = 0)",
                        new { SiteId = siteId.Value, OrderId = orderId });
                }
                catch { /* ignore */ }
            }

            order.SiteId = siteId;
            return orderId;
        }

        public async Task<bool> UpdateStatusAsync(int orderId, byte status)
        {
            using var conn = _factory.CreateConnection();
            var parameters = new DynamicParameters();
            parameters.Add("OrderId", orderId);
            parameters.Add("Status", status);
            parameters.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await conn.ExecuteAsync(
                "koch.spOrders_UpdateStatus",
                parameters,
                commandType: CommandType.StoredProcedure);

            return parameters.Get<int>("Result") == 1;
        }

        public async Task<Order?> GetByIdAsync(int orderId)
        {
            using var conn = _factory.CreateConnection();
            var order = await conn.QueryFirstOrDefaultAsync<Order>(
                "koch.spOrders_GetById",
                new { OrderId = orderId },
                commandType: CommandType.StoredProcedure);

            if (order != null)
            {
                try
                {
                    var wallet = await conn.ExecuteScalarAsync<long?>(
                        "SELECT WalletAmountApplied FROM koch.Orders WHERE Id = @Id", new { Id = orderId });
                    if (wallet.HasValue) order.WalletAmountApplied = wallet;
                }
                catch { /* column missing until Phase1 script */ }

                if (SiteScope.ShouldFilter(_tenant))
                {
                    try
                    {
                        var sid = await conn.ExecuteScalarAsync<int?>(
                            "SELECT SiteId FROM koch.Orders WHERE Id = @Id", new { Id = orderId });
                        order.SiteId = sid;
                        if (sid.HasValue && sid.Value != _tenant.SiteId)
                            return null;
                    }
                    catch { /* no SiteId column yet */ }
                }
            }

            return order;
        }

        public async Task<List<Order>> GetListAsync(DateTime? fromDate, DateTime? toDate, byte? orderType, int? storeId, int? userId, byte? status, int pageSize, int pageIndex)
        {
            using var conn = _factory.CreateConnection();
            var list = (await conn.QueryAsync<Order>(
                "koch.spOrders_List",
                new
                {
                    FromDate = fromDate.HasValue ? fromDate.Value.Date : (DateTime?)null,
                    ToDate = toDate.HasValue ? toDate.Value.Date : (DateTime?)null,
                    OrderType = orderType,
                    StoreId = storeId,
                    UserId = userId,
                    Status = status,
                    PageSize = pageSize,
                    PageIndex = pageIndex
                },
                commandType: CommandType.StoredProcedure)).ToList();

            return await FilterBySiteAsync(conn, list);
        }

        public async Task<int> CountAsync(DateTime? fromDate, DateTime? toDate, byte? orderType, int? storeId, int? userId, byte? status)
        {
            using var conn = _factory.CreateConnection();
            var siteId = SiteScope.CurrentSiteId(_tenant);
            if (siteId.HasValue)
            {
                try
                {
                    return await conn.ExecuteScalarAsync<int>(
                        "koch.spOrders_CountBySite",
                        new
                        {
                            FromDate = fromDate.HasValue ? fromDate.Value.Date : (DateTime?)null,
                            ToDate = toDate.HasValue ? toDate.Value.Date : (DateTime?)null,
                            OrderType = orderType,
                            StoreId = storeId,
                            UserId = userId,
                            Status = status,
                            SiteId = siteId.Value
                        },
                        commandType: CommandType.StoredProcedure);
                }
                catch
                {
                    // fallback below
                }
            }

            return await conn.ExecuteScalarAsync<int>(
                "koch.spOrders_Count",
                new
                {
                    FromDate = fromDate.HasValue ? fromDate.Value.Date : (DateTime?)null,
                    ToDate = toDate.HasValue ? toDate.Value.Date : (DateTime?)null,
                    OrderType = orderType,
                    StoreId = storeId,
                    UserId = userId,
                    Status = status
                },
                commandType: CommandType.StoredProcedure);
        }

        public async Task<List<Order>> GetPendingAsync(int? storeId)
        {
            return await GetPendingScopedAsync(DataScopeQuery.Unrestricted(), storeId);
        }

        public async Task<List<Order>> GetPendingScopedAsync(DataScopeQuery scope, int? storeId)
        {
            return await GetListScopedAsync(scope, null, null, null, storeId, null, 0, 500, 0);
        }

        public async Task<List<Order>> GetListScopedAsync(DataScopeQuery scope, DateTime? fromDate, DateTime? toDate, byte? orderType, int? storeId, int? userId, byte? status, int pageSize, int pageIndex)
        {
            using var conn = _factory.CreateConnection();
            var scopeKind = scope?.Kind ?? "All";
            var scopeValues = scope?.Values != null && scope.Values.Count > 0
                ? string.Join(",", scope.Values) : null;
            var sql = $@"
                SELECT
                    o.[Id],
                    o.[OrderNumber],
                    ISNULL(o.[UserId], o.[CustomerId]) AS [UserId],
                    o.[StoreId],
                    o.[OrderType],
                    o.[SaleChannel],
                    ISNULL(o.[TotalAmount], CAST(ISNULL(o.[TotalPrice], 0) AS BIGINT)) AS [TotalAmount],
                    ISNULL(o.[DiscountAmount], 0) AS [DiscountAmount],
                    ISNULL(o.[FinalAmount], CAST(ISNULL(o.[TotalPrice], 0) AS BIGINT)) AS [FinalAmount],
                    ISNULL(o.[OrderDate], o.[CreatedAt]) AS [OrderDate],
                    CAST(ISNULL(TRY_CAST(o.[Status] AS TINYINT), 0) AS TINYINT) AS [Status],
                    o.[Notes],
                    ISNULL(o.[CreatedAt], o.[OrderDate]) AS [CreatedAt],
                    o.[UpdatedAt],
                    o.[CouponUsageId],
                    o.[ChanceBoxCodeUsageId],
                    LTRIM(RTRIM(ISNULL(u.[FirstName], N'') + N' ' + ISNULL(u.[LastName], N''))) AS [CustomerName],
                    u.[PhoneNumber] AS [CustomerPhone],
                    s.[StoreName]
                FROM koch.Orders o
                LEFT JOIN koch.Users u ON u.[Id] = ISNULL(o.[UserId], o.[CustomerId])
                LEFT JOIN koch.Stores s ON s.StoreID = o.StoreId
                WHERE (@FromDate IS NULL OR CAST(ISNULL(o.[OrderDate], o.[CreatedAt]) AS DATE) >= @FromDate)
                  AND (@ToDate IS NULL OR CAST(ISNULL(o.[OrderDate], o.[CreatedAt]) AS DATE) <= @ToDate)
                  AND (@OrderType IS NULL OR o.[OrderType] = @OrderType)
                  AND (@StoreId IS NULL OR o.[StoreId] = @StoreId)
                  AND (@UserId IS NULL OR o.[UserId] = @UserId OR o.[CustomerId] = @UserId)
                  AND (
                        @Status IS NULL
                        OR TRY_CAST(o.[Status] AS TINYINT) = @Status
                        OR o.[Status] = CAST(@Status AS NVARCHAR(50))
                      )
                  AND {DataScopeSqlBuilder.StoreScopePredicate("s")}
                ORDER BY ISNULL(o.[OrderDate], o.[CreatedAt]) DESC, o.[Id] DESC
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
            var list = (await conn.QueryAsync<Order>(sql, new
            {
                FromDate = fromDate.HasValue ? fromDate.Value.Date : (DateTime?)null,
                ToDate = toDate.HasValue ? toDate.Value.Date : (DateTime?)null,
                OrderType = orderType,
                StoreId = storeId,
                UserId = userId,
                Status = status,
                Offset = pageIndex * pageSize,
                PageSize = pageSize,
                ScopeKind = scopeKind,
                ScopeValues = scopeValues,
                ActorUserId = scope?.ActorUserId
            })).ToList();
            return await FilterBySiteAsync(conn, list);
        }

        public async Task<int> CountScopedAsync(DataScopeQuery scope, DateTime? fromDate, DateTime? toDate, byte? orderType, int? storeId, int? userId, byte? status)
        {
            using var conn = _factory.CreateConnection();
            var scopeKind = scope?.Kind ?? "All";
            var scopeValues = scope?.Values != null && scope.Values.Count > 0
                ? string.Join(",", scope.Values) : null;
            var sql = $@"
                SELECT COUNT(1)
                FROM koch.Orders o
                LEFT JOIN koch.Stores s ON s.StoreID = o.StoreId
                WHERE (@FromDate IS NULL OR CAST(ISNULL(o.[OrderDate], o.[CreatedAt]) AS DATE) >= @FromDate)
                  AND (@ToDate IS NULL OR CAST(ISNULL(o.[OrderDate], o.[CreatedAt]) AS DATE) <= @ToDate)
                  AND (@OrderType IS NULL OR o.[OrderType] = @OrderType)
                  AND (@StoreId IS NULL OR o.[StoreId] = @StoreId)
                  AND (@UserId IS NULL OR o.[UserId] = @UserId OR o.[CustomerId] = @UserId)
                  AND (
                        @Status IS NULL
                        OR TRY_CAST(o.[Status] AS TINYINT) = @Status
                        OR o.[Status] = CAST(@Status AS NVARCHAR(50))
                      )
                  AND {DataScopeSqlBuilder.StoreScopePredicate("s")}";
            return await conn.ExecuteScalarAsync<int>(sql, new
            {
                FromDate = fromDate.HasValue ? fromDate.Value.Date : (DateTime?)null,
                ToDate = toDate.HasValue ? toDate.Value.Date : (DateTime?)null,
                OrderType = orderType,
                StoreId = storeId,
                UserId = userId,
                Status = status,
                ScopeKind = scopeKind,
                ScopeValues = scopeValues,
                ActorUserId = scope?.ActorUserId
            });
        }

        public async Task<List<Order>> GetListByStoreOwnerAsync(int storeOwnerUserId, DateTime? fromDate, DateTime? toDate, byte? orderType, byte? status, string? searchType, string? searchText, int pageSize, int pageIndex)
        {
            using var conn = _factory.CreateConnection();
            var list = (await conn.QueryAsync<Order>(
                "koch.spOrders_ListByStoreOwner",
                new
                {
                    StoreOwnerUserId = storeOwnerUserId,
                    FromDate = fromDate.HasValue ? fromDate.Value.Date : (DateTime?)null,
                    ToDate = toDate.HasValue ? toDate.Value.Date : (DateTime?)null,
                    OrderType = orderType,
                    Status = status,
                    SearchType = searchType,
                    SearchText = searchText,
                    PageSize = pageSize,
                    PageIndex = pageIndex
                },
                commandType: CommandType.StoredProcedure)).ToList();

            return await FilterBySiteAsync(conn, list);
        }

        async Task<List<Order>> FilterBySiteAsync(IDbConnection conn, List<Order> list)
        {
            if (list == null || list.Count == 0 || !SiteScope.ShouldFilter(_tenant))
                return list ?? new List<Order>();

            try
            {
                var ids = list.Select(o => o.Id).Distinct().ToList();
                var allowed = (await conn.QueryAsync<(int Id, int? SiteId)>(
                    "SELECT Id, SiteId FROM koch.Orders WHERE Id IN @Ids",
                    new { Ids = ids })).ToList();

                var map = allowed.ToDictionary(x => x.Id, x => x.SiteId);
                var sid = _tenant.SiteId;
                return list.Where(o =>
                {
                    if (!map.TryGetValue(o.Id, out var orderSite))
                        return true;
                    o.SiteId = orderSite;
                    return orderSite == null || orderSite == sid;
                }).ToList();
            }
            catch
            {
                return list;
            }
        }
    }
}
