using Dapper;
using MyShop.Application.Interfaces.Commerce;
using MyShop.Core.Interfaces;
using MyShop.Infrastructure.Data;
using MyShop.Core.Models.Commerce;
using MyShop.Core.Models.Security;
using MyShop.Core.Security;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Infrastructure.Repositories.Commerce
{
    static class CommerceSchema
    {
        public static async Task<bool> ColumnExistsAsync(IDbConnection conn, string table, string column, IDbTransaction tx = null)
        {
            var n = await conn.ExecuteScalarAsync<int>(
                "SELECT CASE WHEN COL_LENGTH(@Table, @Col) IS NULL THEN 0 ELSE 1 END",
                new { Table = table, Col = column }, tx);
            return n == 1;
        }
    }
    public class CommerceCatalogRepository : ICommerceCatalogRepository
    {
        private readonly IDbConnectionFactory _factory;
        public CommerceCatalogRepository(IDbConnectionFactory factory) => _factory = factory;

        public async Task<List<CommerceShippingMethod>> ListShippingAsync(int? siteId, bool activeOnly = false)
        {
            using var conn = _factory.CreateConnection();
            var rows = await conn.QueryAsync<CommerceShippingMethod>(@"
                SELECT * FROM koch.CommerceShippingMethods
                WHERE (@SiteId IS NULL OR SiteId IS NULL OR SiteId = @SiteId)
                  AND (@ActiveOnly = 0 OR IsActive = 1)
                ORDER BY SortOrder, Id", new { SiteId = siteId, ActiveOnly = activeOnly ? 1 : 0 });
            return rows.ToList();
        }

        public async Task<CommerceShippingMethod> GetShippingAsync(int id)
        {
            using var conn = _factory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<CommerceShippingMethod>(
                "SELECT * FROM koch.CommerceShippingMethods WHERE Id=@Id", new { Id = id });
        }

        public async Task<int> UpsertShippingAsync(CommerceShippingMethod m)
        {
            using var conn = _factory.CreateConnection();
            if (m.Id > 0)
            {
                var hasFree = await CommerceSchema.ColumnExistsAsync(conn, "koch.CommerceShippingMethods", "FreeOverAmount");
                var sql = @"
                    UPDATE koch.CommerceShippingMethods
                    SET SiteId=@SiteId, Title=@Title, Description=@Description, Fee=@Fee,
                        MinOrderAmount=@MinOrderAmount, EstimatedDays=@EstimatedDays,
                        SortOrder=@SortOrder, IsActive=@IsActive, UpdatedAt=SYSDATETIME()";
                if (hasFree) sql += ", FreeOverAmount=@FreeOverAmount";
                sql += " WHERE Id=@Id";
                await conn.ExecuteAsync(sql, m);
                return m.Id;
            }
            {
                var hasFree = await CommerceSchema.ColumnExistsAsync(conn, "koch.CommerceShippingMethods", "FreeOverAmount");
                if (hasFree)
                    return await conn.ExecuteScalarAsync<int>(@"
                        INSERT INTO koch.CommerceShippingMethods
                            (SiteId, Title, Description, Fee, MinOrderAmount, FreeOverAmount, EstimatedDays, SortOrder, IsActive)
                        OUTPUT INSERTED.Id
                        VALUES (@SiteId, @Title, @Description, @Fee, @MinOrderAmount, @FreeOverAmount, @EstimatedDays, @SortOrder, @IsActive)", m);
                return await conn.ExecuteScalarAsync<int>(@"
                    INSERT INTO koch.CommerceShippingMethods
                        (SiteId, Title, Description, Fee, MinOrderAmount, EstimatedDays, SortOrder, IsActive)
                    OUTPUT INSERTED.Id
                    VALUES (@SiteId, @Title, @Description, @Fee, @MinOrderAmount, @EstimatedDays, @SortOrder, @IsActive)", m);
            }
        }

        public async Task DeleteShippingAsync(int id)
        {
            using var conn = _factory.CreateConnection();
            await ReferenceCount.EnsureUnusedAsync(conn, new { Id = id },
                ("SELECT COUNT(1) FROM koch.Orders WHERE ShippingMethodId=@Id", "سفارش"));
            await conn.ExecuteAsync("DELETE FROM koch.CommerceShippingMethods WHERE Id=@Id", new { Id = id });
        }

        public async Task<List<CommercePaymentMethod>> ListPaymentAsync(int? siteId, bool activeOnly = false)
        {
            using var conn = _factory.CreateConnection();
            var rows = await conn.QueryAsync<CommercePaymentMethod>(@"
                SELECT * FROM koch.CommercePaymentMethods
                WHERE (@SiteId IS NULL OR SiteId IS NULL OR SiteId = @SiteId)
                  AND (@ActiveOnly = 0 OR IsActive = 1)
                ORDER BY SortOrder, Id", new { SiteId = siteId, ActiveOnly = activeOnly ? 1 : 0 });
            return rows.ToList();
        }

        public async Task<CommercePaymentMethod> GetPaymentAsync(int id)
        {
            using var conn = _factory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<CommercePaymentMethod>(
                "SELECT * FROM koch.CommercePaymentMethods WHERE Id=@Id", new { Id = id });
        }

        public async Task<int> UpsertPaymentAsync(CommercePaymentMethod m)
        {
            using var conn = _factory.CreateConnection();
            if (m.Id > 0)
            {
                await conn.ExecuteAsync(@"
                    UPDATE koch.CommercePaymentMethods
                    SET SiteId=@SiteId, Title=@Title, Code=@Code, GatewayProvider=@GatewayProvider,
                        Description=@Description, ConfigJson=@ConfigJson, SortOrder=@SortOrder,
                        IsActive=@IsActive, UpdatedAt=SYSDATETIME()
                    WHERE Id=@Id", m);
                return m.Id;
            }
            return await conn.ExecuteScalarAsync<int>(@"
                INSERT INTO koch.CommercePaymentMethods
                    (SiteId, Title, Code, GatewayProvider, Description, ConfigJson, SortOrder, IsActive)
                OUTPUT INSERTED.Id
                VALUES (@SiteId, @Title, @Code, @GatewayProvider, @Description, @ConfigJson, @SortOrder, @IsActive)", m);
        }

        public async Task DeletePaymentAsync(int id)
        {
            using var conn = _factory.CreateConnection();
            await ReferenceCount.EnsureUnusedAsync(conn, new { Id = id },
                ("SELECT COUNT(1) FROM koch.Orders WHERE PaymentMethodId=@Id", "سفارش"));
            await conn.ExecuteAsync("DELETE FROM koch.CommercePaymentMethods WHERE Id=@Id", new { Id = id });
        }

        public async Task<List<CommerceDeliverySlot>> ListSlotsAsync(int? siteId, bool activeOnly = false)
        {
            using var conn = _factory.CreateConnection();
            var rows = await conn.QueryAsync<CommerceDeliverySlot>(@"
                SELECT * FROM koch.CommerceDeliverySlots
                WHERE (@SiteId IS NULL OR SiteId IS NULL OR SiteId = @SiteId)
                  AND (@ActiveOnly = 0 OR IsActive = 1)
                ORDER BY SortOrder, Id", new { SiteId = siteId, ActiveOnly = activeOnly ? 1 : 0 });
            return rows.ToList();
        }

        public async Task<CommerceDeliverySlot> GetSlotAsync(int id)
        {
            using var conn = _factory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<CommerceDeliverySlot>(
                "SELECT * FROM koch.CommerceDeliverySlots WHERE Id=@Id", new { Id = id });
        }

        public async Task<int> UpsertSlotAsync(CommerceDeliverySlot m)
        {
            using var conn = _factory.CreateConnection();
            if (m.Id > 0)
            {
                await conn.ExecuteAsync(@"
                    UPDATE koch.CommerceDeliverySlots
                    SET SiteId=@SiteId, Title=@Title, DayOfWeek=@DayOfWeek, StartTime=@StartTime,
                        EndTime=@EndTime, Capacity=@Capacity, SortOrder=@SortOrder, IsActive=@IsActive
                    WHERE Id=@Id", m);
                return m.Id;
            }
            return await conn.ExecuteScalarAsync<int>(@"
                INSERT INTO koch.CommerceDeliverySlots
                    (SiteId, Title, DayOfWeek, StartTime, EndTime, Capacity, SortOrder, IsActive)
                OUTPUT INSERTED.Id
                VALUES (@SiteId, @Title, @DayOfWeek, @StartTime, @EndTime, @Capacity, @SortOrder, @IsActive)", m);
        }

        public async Task DeleteSlotAsync(int id)
        {
            using var conn = _factory.CreateConnection();
            await ReferenceCount.EnsureUnusedAsync(conn, new { Id = id },
                ("SELECT COUNT(1) FROM koch.Orders WHERE DeliverySlotId=@Id", "سفارش"));
            await conn.ExecuteAsync("DELETE FROM koch.CommerceDeliverySlots WHERE Id=@Id", new { Id = id });
        }
    }

    public class CommerceCartRepository : ICommerceCartRepository
    {
        private readonly IDbConnectionFactory _factory;
        public CommerceCartRepository(IDbConnectionFactory factory) => _factory = factory;

        public async Task<CommerceCart> GetOrCreateAsync(int? siteId, int? userId, string guestKey)
        {
            using var conn = _factory.CreateConnection();
            CommerceCart cart = null;
            if (userId.HasValue && userId.Value > 0)
            {
                cart = await conn.QueryFirstOrDefaultAsync<CommerceCart>(@"
                    SELECT TOP 1 * FROM koch.CommerceCarts
                    WHERE UserId=@UserId AND (@SiteId IS NULL OR SiteId=@SiteId OR SiteId IS NULL)
                    ORDER BY Id DESC", new { UserId = userId, SiteId = siteId });
            }
            else if (!string.IsNullOrWhiteSpace(guestKey))
            {
                cart = await conn.QueryFirstOrDefaultAsync<CommerceCart>(@"
                    SELECT TOP 1 * FROM koch.CommerceCarts
                    WHERE GuestKey=@GuestKey AND (@SiteId IS NULL OR SiteId=@SiteId OR SiteId IS NULL)
                    ORDER BY Id DESC", new { GuestKey = guestKey, SiteId = siteId });
            }

            if (cart == null)
            {
                var id = await conn.ExecuteScalarAsync<int>(@"
                    INSERT INTO koch.CommerceCarts (SiteId, UserId, GuestKey)
                    OUTPUT INSERTED.Id
                    VALUES (@SiteId, @UserId, @GuestKey)",
                    new { SiteId = siteId, UserId = userId, GuestKey = guestKey });
                cart = new CommerceCart { Id = id, SiteId = siteId, UserId = userId, GuestKey = guestKey };
            }

            return await GetWithItemsAsync(cart.Id);
        }

        public async Task<CommerceCart> GetWithItemsAsync(int cartId)
        {
            using var conn = _factory.CreateConnection();
            var cart = await conn.QueryFirstOrDefaultAsync<CommerceCart>(
                "SELECT * FROM koch.CommerceCarts WHERE Id=@Id", new { Id = cartId });
            if (cart == null) return null;
            var items = await conn.QueryAsync<CommerceCartItem>(
                "SELECT * FROM koch.CommerceCartItems WHERE CartId=@CartId ORDER BY Id", new { CartId = cartId });
            cart.Items = items.ToList();
            return cart;
        }

        public async Task SetItemAsync(int cartId, int productId, string title, long unitPrice, int quantity)
        {
            using var conn = _factory.CreateConnection();
            if (quantity <= 0)
            {
                await RemoveItemAsync(cartId, productId);
                return;
            }
            await conn.ExecuteAsync(@"
                MERGE koch.CommerceCartItems AS t
                USING (SELECT @CartId CartId, @ProductId ProductId) AS s
                ON t.CartId = s.CartId AND t.ProductId = s.ProductId
                WHEN MATCHED THEN UPDATE SET Quantity=@Quantity, UnitPrice=@UnitPrice, Title=@Title
                WHEN NOT MATCHED THEN INSERT (CartId, ProductId, Title, UnitPrice, Quantity)
                    VALUES (@CartId, @ProductId, @Title, @UnitPrice, @Quantity);
                UPDATE koch.CommerceCarts SET UpdatedAt=SYSDATETIME() WHERE Id=@CartId;",
                new { CartId = cartId, ProductId = productId, Title = title, UnitPrice = unitPrice, Quantity = quantity });
        }

        public async Task RemoveItemAsync(int cartId, int productId)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(
                "DELETE FROM koch.CommerceCartItems WHERE CartId=@CartId AND ProductId=@ProductId",
                new { CartId = cartId, ProductId = productId });
        }

        public async Task ClearAsync(int cartId)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync("DELETE FROM koch.CommerceCartItems WHERE CartId=@CartId", new { CartId = cartId });
        }

        public async Task AttachUserAsync(int cartId, int userId)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(
                "UPDATE koch.CommerceCarts SET UserId=@UserId, UpdatedAt=SYSDATETIME() WHERE Id=@Id",
                new { Id = cartId, UserId = userId });
        }
    }

    public class CommerceOrderRepository : ICommerceOrderRepository
    {
        private readonly IDbConnectionFactory _factory;
        public CommerceOrderRepository(IDbConnectionFactory factory) => _factory = factory;

        public async Task<int> CreateCommerceOrderAsync(CommerceOrderDetail order, IEnumerable<OrderItem> items, int? changedByUserId)
        {
            using var conn = _factory.CreateConnection();
            if (conn.State != System.Data.ConnectionState.Open) conn.Open();
            using var tx = conn.BeginTransaction();

            if (string.IsNullOrWhiteSpace(order.OrderNumber))
                order.OrderNumber = "C" + DateTime.Now.ToString("yyyyMMddHHmmss") + new Random().Next(100, 999);

            var hasTax = await CommerceSchema.ColumnExistsAsync(conn, "koch.Orders", "TaxAmount", tx);
            var orderCols = @"OrderNumber, UserId, StoreId, OrderType, SaleChannel, TotalAmount, DiscountAmount, FinalAmount,
                 OrderDate, Status, Notes, SiteId, ShippingMethodId, PaymentMethodId, DeliverySlotId, DeliveryDate,
                 ShippingFee, ShippingAddress, TrackingCode, FulfillmentStatus, PaymentStatus, IsCommerce";
            var orderVals = @"@OrderNumber, @UserId, @StoreId, 0, 0, @TotalAmount, @DiscountAmount, @FinalAmount,
                 SYSDATETIME(), 0, @Notes, @SiteId, @ShippingMethodId, @PaymentMethodId, @DeliverySlotId, @DeliveryDate,
                 @ShippingFee, @ShippingAddress, @TrackingCode, @FulfillmentStatus, @PaymentStatus, 1";
            if (hasTax)
            {
                orderCols += ", TaxAmount";
                orderVals += ", @TaxAmount";
            }
            var orderId = await conn.ExecuteScalarAsync<int>(
                $"INSERT INTO koch.Orders ({orderCols}) OUTPUT INSERTED.Id VALUES ({orderVals})",
                order, tx);

            var cols = await LoadOrderItemColumnsAsync(conn, tx);
            foreach (var it in items ?? Enumerable.Empty<OrderItem>())
            {
                it.OrderId = orderId;
                await InsertOrderItemAsync(conn, tx, it, cols);
            }

            await conn.ExecuteAsync(@"
                INSERT INTO koch.OrderStatusHistory (OrderId, FromStatus, ToStatus, Note, ChangedByUserId)
                VALUES (@OrderId, NULL, @ToStatus, @Note, @ChangedBy)",
                new { OrderId = orderId, ToStatus = order.FulfillmentStatus, Note = "ثبت سفارش فروشگاهی", ChangedBy = changedByUserId }, tx);

            tx.Commit();
            return orderId;
        }

        public async Task<bool> OrderItemColumnExistsAsync(string columnName)
        {
            if (string.IsNullOrWhiteSpace(columnName)) return false;
            using var conn = _factory.CreateConnection();
            var n = await conn.ExecuteScalarAsync<int>(
                "SELECT CASE WHEN COL_LENGTH(N'koch.OrderItems', @Name) IS NULL THEN 0 ELSE 1 END",
                new { Name = columnName });
            return n == 1;
        }

        public async Task<bool> OrderColumnExistsAsync(string columnName)
        {
            if (string.IsNullOrWhiteSpace(columnName)) return false;
            using var conn = _factory.CreateConnection();
            return await CommerceSchema.ColumnExistsAsync(conn, "koch.Orders", columnName);
        }

        static async Task<HashSet<string>> LoadOrderItemColumnsAsync(IDbConnection conn, IDbTransaction tx)
        {
            var names = await conn.QueryAsync<string>(
                "SELECT name FROM sys.columns WHERE object_id = OBJECT_ID(N'koch.OrderItems')",
                transaction: tx);
            return new HashSet<string>(names ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase);
        }

        static async Task InsertOrderItemAsync(IDbConnection conn, IDbTransaction tx, OrderItem it, HashSet<string> colsPresent)
        {
            var cols = new List<string> { "OrderId", "ProductId", "Quantity", "UnitPrice" };
            var vals = new List<string> { "@OrderId", "@ProductId", "@Quantity", "@UnitPrice" };
            if (colsPresent.Contains("Title")) { cols.Add("Title"); vals.Add("@Title"); }
            if (colsPresent.Contains("LineTotal")) { cols.Add("LineTotal"); vals.Add("@LineTotal"); }
            else if (colsPresent.Contains("TotalPrice")) { cols.Add("TotalPrice"); vals.Add("@LineTotal"); }
            if (colsPresent.Contains("StoreId")) { cols.Add("StoreId"); vals.Add("@StoreId"); }
            if (colsPresent.Contains("VariationId")) { cols.Add("VariationId"); vals.Add("@VariationId"); }
            if (colsPresent.Contains("Sku")) { cols.Add("Sku"); vals.Add("@Sku"); }
            if (colsPresent.Contains("DiscountAmount")) { cols.Add("DiscountAmount"); vals.Add("@DiscountAmount"); }

            var sql = $"INSERT INTO koch.OrderItems ({string.Join(",", cols)}) VALUES ({string.Join(",", vals)})";
            await conn.ExecuteAsync(sql, it, tx);
        }

        public async Task<CommerceOrderDetail> GetDetailAsync(int orderId)
        {
            using var conn = _factory.CreateConnection();
            var o = await conn.QueryFirstOrDefaultAsync<CommerceOrderDetail>(@"
                SELECT o.Id, o.OrderNumber, o.UserId, o.StoreId, o.SiteId,
                       o.TotalAmount, o.DiscountAmount, ISNULL(o.ShippingFee,0) ShippingFee, o.FinalAmount,
                       o.ShippingMethodId, o.PaymentMethodId, o.DeliverySlotId, o.DeliveryDate,
                       o.ShippingAddress, o.TrackingCode,
                       ISNULL(o.FulfillmentStatus,0) FulfillmentStatus, ISNULL(o.PaymentStatus,0) PaymentStatus,
                       ISNULL(o.IsCommerce,0) IsCommerce, o.OrderDate, o.Notes,
                       LTRIM(RTRIM(ISNULL(u.FirstName,'') + ' ' + ISNULL(u.LastName,''))) CustomerName,
                       u.PhoneNumber CustomerPhone, st.StoreName,
                       sm.Title ShippingMethodTitle, pm.Title PaymentMethodTitle, ds.Title DeliverySlotTitle
                FROM koch.Orders o
                LEFT JOIN koch.Users u ON u.Id = o.UserId
                LEFT JOIN koch.Stores st ON st.StoreID = o.StoreId
                LEFT JOIN koch.CommerceShippingMethods sm ON sm.Id = o.ShippingMethodId
                LEFT JOIN koch.CommercePaymentMethods pm ON pm.Id = o.PaymentMethodId
                LEFT JOIN koch.CommerceDeliverySlots ds ON ds.Id = o.DeliverySlotId
                WHERE o.Id = @Id", new { Id = orderId });
            if (o == null) return null;
            try
            {
                if (await CommerceSchema.ColumnExistsAsync(conn, "koch.Orders", "TaxAmount"))
                    o.TaxAmount = await conn.ExecuteScalarAsync<long>(
                        "SELECT ISNULL(TaxAmount,0) FROM koch.Orders WHERE Id=@Id", new { Id = orderId });
            }
            catch { o.TaxAmount = 0; }
            o.Items = (await conn.QueryAsync<OrderItem>(
                "SELECT * FROM koch.OrderItems WHERE OrderId=@Id ORDER BY Id", new { Id = orderId })).ToList();
            o.History = await ListHistoryAsync(orderId);
            return o;
        }

        public async Task<List<CommerceOrderDetail>> ListAsync(int? siteId = null, int? userId = null, int? storeId = null, int top = 200)
        {
            return await ListScopedAsync(DataScopeQuery.Unrestricted(), siteId, userId, storeId, top);
        }

        public async Task<List<CommerceOrderDetail>> ListScopedAsync(DataScopeQuery scope, int? siteId = null, int? userId = null, int? storeId = null, int top = 200)
        {
            using var conn = _factory.CreateConnection();
            var scopeKind = scope?.Kind ?? "All";
            var scopeValues = scope?.Values != null && scope.Values.Count > 0
                ? string.Join(",", scope.Values) : null;
            var rows = await conn.QueryAsync<CommerceOrderDetail>($@"
                SELECT TOP (@Top)
                       o.Id, o.OrderNumber, o.UserId, o.StoreId, o.SiteId,
                       o.TotalAmount, o.DiscountAmount, ISNULL(o.ShippingFee,0) ShippingFee, o.FinalAmount,
                       o.ShippingMethodId, o.PaymentMethodId, o.DeliverySlotId, o.DeliveryDate,
                       o.ShippingAddress, o.TrackingCode,
                       ISNULL(o.FulfillmentStatus,0) FulfillmentStatus, ISNULL(o.PaymentStatus,0) PaymentStatus,
                       ISNULL(o.IsCommerce,0) IsCommerce, o.OrderDate, o.Notes,
                       LTRIM(RTRIM(ISNULL(u.FirstName,'') + ' ' + ISNULL(u.LastName,''))) CustomerName,
                       u.PhoneNumber CustomerPhone, st.StoreName,
                       sm.Title ShippingMethodTitle, pm.Title PaymentMethodTitle, ds.Title DeliverySlotTitle
                FROM koch.Orders o
                LEFT JOIN koch.Users u ON u.Id = o.UserId
                LEFT JOIN koch.Stores st ON st.StoreID = o.StoreId
                LEFT JOIN koch.CommerceShippingMethods sm ON sm.Id = o.ShippingMethodId
                LEFT JOIN koch.CommercePaymentMethods pm ON pm.Id = o.PaymentMethodId
                LEFT JOIN koch.CommerceDeliverySlots ds ON ds.Id = o.DeliverySlotId
                WHERE ISNULL(o.IsCommerce,0) = 1
                  AND (@SiteId IS NULL OR o.SiteId IS NULL OR o.SiteId = @SiteId)
                  AND (@UserId IS NULL OR o.UserId = @UserId)
                  AND (@StoreId IS NULL OR o.StoreId = @StoreId)
                  AND {DataScopeSqlBuilder.StoreScopePredicate("st")}
                ORDER BY o.Id DESC",
                new
                {
                    Top = top,
                    SiteId = siteId,
                    UserId = userId,
                    StoreId = storeId,
                    ScopeKind = scopeKind,
                    ScopeValues = scopeValues,
                    ActorUserId = scope?.ActorUserId
                });
            return rows.ToList();
        }

        public async Task<List<CommerceOrderDetail>> ListForStoreIdsAsync(IReadOnlyList<int> storeIds, int top = 200)
        {
            if (storeIds == null || storeIds.Count == 0)
                return new List<CommerceOrderDetail>();
            using var conn = _factory.CreateConnection();
            var hasLineStore = await conn.ExecuteScalarAsync<int>(
                "SELECT CASE WHEN COL_LENGTH(N'koch.OrderItems', N'StoreId') IS NULL THEN 0 ELSE 1 END");
            var lineClause = hasLineStore == 1
                ? @" AND (
                        EXISTS (SELECT 1 FROM koch.OrderItems oi WHERE oi.OrderId = o.Id AND oi.StoreId IN @StoreIds)
                        OR (
                            NOT EXISTS (SELECT 1 FROM koch.OrderItems oi2 WHERE oi2.OrderId = o.Id AND oi2.StoreId IS NOT NULL AND oi2.StoreId > 0)
                            AND o.StoreId IN @StoreIds
                        )
                    )"
                : " AND o.StoreId IN @StoreIds";
            var rows = await conn.QueryAsync<CommerceOrderDetail>(@"
                SELECT TOP (@Top)
                       o.Id, o.OrderNumber, o.UserId, o.StoreId, o.SiteId,
                       o.TotalAmount, o.DiscountAmount, ISNULL(o.ShippingFee,0) ShippingFee, o.FinalAmount,
                       o.ShippingMethodId, o.PaymentMethodId, o.DeliverySlotId, o.DeliveryDate,
                       o.ShippingAddress, o.TrackingCode,
                       ISNULL(o.FulfillmentStatus,0) FulfillmentStatus, ISNULL(o.PaymentStatus,0) PaymentStatus,
                       ISNULL(o.IsCommerce,0) IsCommerce, o.OrderDate, o.Notes,
                       LTRIM(RTRIM(ISNULL(u.FirstName,'') + ' ' + ISNULL(u.LastName,''))) CustomerName,
                       u.PhoneNumber CustomerPhone, st.StoreName,
                       sm.Title ShippingMethodTitle, pm.Title PaymentMethodTitle, ds.Title DeliverySlotTitle
                FROM koch.Orders o
                LEFT JOIN koch.Users u ON u.Id = o.UserId
                LEFT JOIN koch.Stores st ON st.StoreID = o.StoreId
                LEFT JOIN koch.CommerceShippingMethods sm ON sm.Id = o.ShippingMethodId
                LEFT JOIN koch.CommercePaymentMethods pm ON pm.Id = o.PaymentMethodId
                LEFT JOIN koch.CommerceDeliverySlots ds ON ds.Id = o.DeliverySlotId
                WHERE ISNULL(o.IsCommerce,0) = 1
                " + lineClause + @"
                ORDER BY o.Id DESC",
                new { Top = top, StoreIds = storeIds.ToArray() });
            return rows.ToList();
        }

        public async Task UpdateFulfillmentAsync(int orderId, byte status, string trackingCode, string note, int? changedByUserId)
        {
            using var conn = _factory.CreateConnection();
            if (conn.State != System.Data.ConnectionState.Open) conn.Open();
            using var tx = conn.BeginTransaction();
            var current = await conn.ExecuteScalarAsync<byte?>(
                "SELECT FulfillmentStatus FROM koch.Orders WHERE Id=@Id", new { Id = orderId }, tx);
            await conn.ExecuteAsync(@"
                UPDATE koch.Orders
                SET FulfillmentStatus=@Status,
                    TrackingCode=COALESCE(@TrackingCode, TrackingCode),
                    UpdatedAt=GETDATE(),
                    Status = CASE WHEN @Status IN (5,6) THEN CASE WHEN @Status=5 THEN 1 ELSE 2 END ELSE Status END
                WHERE Id=@Id",
                new { Id = orderId, Status = status, TrackingCode = trackingCode }, tx);
            await conn.ExecuteAsync(@"
                INSERT INTO koch.OrderStatusHistory (OrderId, FromStatus, ToStatus, Note, ChangedByUserId)
                VALUES (@OrderId, @FromStatus, @ToStatus, @Note, @ChangedBy)",
                new { OrderId = orderId, FromStatus = current, ToStatus = status, Note = note, ChangedBy = changedByUserId }, tx);
            tx.Commit();
        }

        public async Task UpdatePaymentStatusAsync(int orderId, byte paymentStatus, byte? fulfillmentStatus = null)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(@"
                UPDATE koch.Orders
                SET PaymentStatus=@PaymentStatus,
                    FulfillmentStatus=COALESCE(@FulfillmentStatus, FulfillmentStatus),
                    UpdatedAt=GETDATE()
                WHERE Id=@Id",
                new { Id = orderId, PaymentStatus = paymentStatus, FulfillmentStatus = fulfillmentStatus });
        }

        public async Task<List<OrderStatusHistory>> ListHistoryAsync(int orderId)
        {
            using var conn = _factory.CreateConnection();
            var rows = await conn.QueryAsync<OrderStatusHistory>(@"
                SELECT h.*, LTRIM(RTRIM(ISNULL(u.FirstName,'') + ' ' + ISNULL(u.LastName,''))) ChangedByName
                FROM koch.OrderStatusHistory h
                LEFT JOIN koch.Users u ON u.Id = h.ChangedByUserId
                WHERE h.OrderId=@OrderId
                ORDER BY h.Id", new { OrderId = orderId });
            return rows.ToList();
        }

        public async Task<int> InsertPaymentAsync(CommercePayment payment)
        {
            using var conn = _factory.CreateConnection();
            return await conn.ExecuteScalarAsync<int>(@"
                INSERT INTO koch.CommercePayments
                    (OrderId, PaymentMethodId, Amount, Provider, Authority, RefId, Status, RawResponse)
                OUTPUT INSERTED.Id
                VALUES (@OrderId, @PaymentMethodId, @Amount, @Provider, @Authority, @RefId, @Status, @RawResponse)", payment);
        }

        public async Task UpdatePaymentAsync(CommercePayment payment)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(@"
                UPDATE koch.CommercePayments
                SET Status=@Status, RefId=@RefId, RawResponse=@RawResponse, PaidAt=@PaidAt
                WHERE Id=@Id", payment);
        }

        public async Task<CommercePayment> GetPaymentByAuthorityAsync(string authority)
        {
            using var conn = _factory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<CommercePayment>(
                "SELECT TOP 1 * FROM koch.CommercePayments WHERE Authority=@Authority ORDER BY Id DESC",
                new { Authority = authority });
        }

        public async Task<CommercePayment> GetPaymentByIdAsync(int paymentId)
        {
            using var conn = _factory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<CommercePayment>(
                "SELECT * FROM koch.CommercePayments WHERE Id=@Id", new { Id = paymentId });
        }

        public async Task<bool> TryMarkPaymentPaidAsync(int paymentId, string refId, string rawResponse)
        {
            using var conn = _factory.CreateConnection();
            var n = await conn.ExecuteAsync(@"
                UPDATE koch.CommercePayments
                SET Status=@Paid, RefId=@RefId, RawResponse=@RawResponse, PaidAt=SYSDATETIME()
                WHERE Id=@Id AND Status IN (@Init, @Pending, @Failed)",
                new
                {
                    Id = paymentId,
                    RefId = refId,
                    RawResponse = rawResponse,
                    Paid = CommercePaymentStatus.Paid,
                    Init = CommercePaymentStatus.Unpaid,
                    Pending = CommercePaymentStatus.Pending,
                    Failed = CommercePaymentStatus.Failed
                });
            return n > 0;
        }

        public async Task<bool> TryBeginPaymentFinalizationAsync(int paymentId)
        {
            using var conn = _factory.CreateConnection();
            var n = await conn.ExecuteAsync(@"
                UPDATE koch.CommercePayments
                SET FinalizedState=@InProgress
                WHERE Id=@Id AND Status=@Paid AND FinalizedState=@None",
                new
                {
                    Id = paymentId,
                    Paid = CommercePaymentStatus.Paid,
                    InProgress = CommercePaymentFinalizedState.InProgress,
                    None = CommercePaymentFinalizedState.None
                });
            return n > 0;
        }

        public async Task MarkPaymentFinalizationDoneAsync(int paymentId)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(@"
                UPDATE koch.CommercePayments SET FinalizedState=@Done WHERE Id=@Id",
                new { Id = paymentId, Done = CommercePaymentFinalizedState.Done });
        }

        public async Task AbandonPaymentFinalizationAsync(int paymentId)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(@"
                UPDATE koch.CommercePayments SET FinalizedState=@None
                WHERE Id=@Id AND FinalizedState=@InProgress",
                new
                {
                    Id = paymentId,
                    None = CommercePaymentFinalizedState.None,
                    InProgress = CommercePaymentFinalizedState.InProgress
                });
        }

        public async Task<bool> TryMarkPaymentFailedIfNotPaidAsync(int paymentId, string rawResponse)
        {
            using var conn = _factory.CreateConnection();
            var n = await conn.ExecuteAsync(@"
                UPDATE koch.CommercePayments
                SET Status=@Failed, RawResponse=@RawResponse
                WHERE Id=@Id AND Status IN (@Init, @Pending)",
                new
                {
                    Id = paymentId,
                    RawResponse = rawResponse,
                    Failed = CommercePaymentStatus.Failed,
                    Init = CommercePaymentStatus.Unpaid,
                    Pending = CommercePaymentStatus.Pending
                });
            return n > 0;
        }

        public async Task InsertPaymentAttemptAsync(CommercePaymentAttempt attempt)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(@"
                INSERT INTO koch.CommercePaymentAttempts
                    (PaymentId, AttemptType, Authority, ReferenceId, Status, ResponseCode, ProcessedAt)
                VALUES (@PaymentId, @AttemptType, @Authority, @ReferenceId, @Status, @ResponseCode, SYSDATETIME())",
                attempt);
        }

        public async Task<CommerceDashboardKpis> GetDashboardKpisAsync(int? siteId)
        {
            using var conn = _factory.CreateConnection();
            var kpi = await conn.QueryFirstOrDefaultAsync<CommerceDashboardKpis>(@"
                SELECT
                  SUM(CASE WHEN CAST(o.OrderDate AS DATE)=CAST(GETDATE() AS DATE) THEN 1 ELSE 0 END) OrdersToday,
                  SUM(CASE WHEN o.OrderDate >= DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1) THEN 1 ELSE 0 END) OrdersMonth,
                  SUM(CASE WHEN CAST(o.OrderDate AS DATE)=CAST(GETDATE() AS DATE) AND ISNULL(o.PaymentStatus,0)=2 THEN o.FinalAmount ELSE 0 END) RevenueToday,
                  SUM(CASE WHEN o.OrderDate >= DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1) AND ISNULL(o.PaymentStatus,0) IN (0,2) AND ISNULL(o.FulfillmentStatus,0) NOT IN (6,7) THEN o.FinalAmount ELSE 0 END) RevenueMonth,
                  SUM(CASE WHEN ISNULL(o.FulfillmentStatus,0)=1 THEN 1 ELSE 0 END) AwaitingPayment,
                  SUM(CASE WHEN ISNULL(o.FulfillmentStatus,0) IN (2,3) THEN 1 ELSE 0 END) Processing,
                  SUM(CASE WHEN ISNULL(o.FulfillmentStatus,0)=4 THEN 1 ELSE 0 END) Shipped
                FROM koch.Orders o
                WHERE ISNULL(o.IsCommerce,0)=1
                  AND (@SiteId IS NULL OR o.SiteId IS NULL OR o.SiteId=@SiteId)",
                new { SiteId = siteId }) ?? new CommerceDashboardKpis();

            var cartRow = await conn.QueryFirstOrDefaultAsync<dynamic>(@"
                SELECT
                  COUNT(DISTINCT c.Id) AS ActiveCarts,
                  ISNULL(SUM(i.Quantity),0) AS CartItemsTotal
                FROM koch.CommerceCarts c
                INNER JOIN koch.CommerceCartItems i ON i.CartId=c.Id
                WHERE c.UpdatedAt >= DATEADD(DAY,-14,SYSDATETIME())
                  AND (@SiteId IS NULL OR c.SiteId IS NULL OR c.SiteId=@SiteId)",
                new { SiteId = siteId });
            if (cartRow != null)
            {
                try { kpi.ActiveCarts = (int)cartRow.ActiveCarts; } catch { }
                try { kpi.CartItemsTotal = (int)cartRow.CartItemsTotal; } catch { }
            }
            return kpi;
        }
    }
}
