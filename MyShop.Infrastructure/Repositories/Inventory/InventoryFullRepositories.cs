using Dapper;
using MyShop.Application.Interfaces.Inventory;
using MyShop.Core.Interfaces;
using MyShop.Core.Models.Inventory;
using MyShop.Core.Models.Security;
using MyShop.Core.Security;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Infrastructure.Repository
{
    public class ProductUnitRepository : IProductUnitRepository
    {
        private readonly IDbConnectionFactory _factory;
        public ProductUnitRepository(IDbConnectionFactory factory) => _factory = factory;

        public async Task<List<ProductUnit>> GetAllAsync(bool? onlyActive = null)
        {
            using var conn = _factory.CreateConnection();
            var sql = "SELECT * FROM koch.ProductUnits WHERE (@OnlyActive IS NULL OR IsActive = @OnlyActive) ORDER BY SortOrder, Name";
            return (await conn.QueryAsync<ProductUnit>(sql, new { OnlyActive = onlyActive })).ToList();
        }

        public async Task<ProductUnit> GetByIdAsync(int id)
        {
            using var conn = _factory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<ProductUnit>("SELECT * FROM koch.ProductUnits WHERE Id=@Id", new { Id = id });
        }

        public async Task<int> UpsertAsync(ProductUnit unit)
        {
            using var conn = _factory.CreateConnection();
            if (unit.Id > 0)
            {
                await conn.ExecuteAsync(
                    @"UPDATE koch.ProductUnits SET Name=@Name, Symbol=@Symbol, IsActive=@IsActive, SortOrder=@SortOrder WHERE Id=@Id",
                    unit);
                return unit.Id;
            }
            return await conn.ExecuteScalarAsync<int>(
                @"INSERT INTO koch.ProductUnits (Name, Symbol, IsActive, SortOrder) OUTPUT INSERTED.Id
                  VALUES (@Name, @Symbol, @IsActive, @SortOrder)", unit);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var conn = _factory.CreateConnection();
            return await conn.ExecuteAsync("DELETE FROM koch.ProductUnits WHERE Id=@Id", new { Id = id }) > 0;
        }
    }

    public class PriceListRepository : IPriceListRepository
    {
        private readonly IDbConnectionFactory _factory;
        public PriceListRepository(IDbConnectionFactory factory) => _factory = factory;

        public async Task<List<PriceList>> GetAllAsync(bool? onlyActive = null)
        {
            using var conn = _factory.CreateConnection();
            return (await conn.QueryAsync<PriceList>(
                "SELECT * FROM koch.Prices WHERE (@OnlyActive IS NULL OR IsActive=@OnlyActive) ORDER BY SortOrder, Name",
                new { OnlyActive = onlyActive })).ToList();
        }

        public async Task<PriceList> GetByIdAsync(int id)
        {
            using var conn = _factory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<PriceList>("SELECT * FROM koch.Prices WHERE Id=@Id", new { Id = id });
        }

        public async Task<int> UpsertAsync(PriceList price)
        {
            using var conn = _factory.CreateConnection();
            if (price.Id > 0)
            {
                await conn.ExecuteAsync(
                    @"UPDATE koch.Prices SET Name=@Name, Code=@Code, Description=@Description, IsActive=@IsActive, SortOrder=@SortOrder WHERE Id=@Id",
                    price);
                return price.Id;
            }
            return await conn.ExecuteScalarAsync<int>(
                @"INSERT INTO koch.Prices (Name, Code, Description, IsActive, SortOrder) OUTPUT INSERTED.Id
                  VALUES (@Name, @Code, @Description, @IsActive, @SortOrder)", price);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var conn = _factory.CreateConnection();
            return await conn.ExecuteAsync("DELETE FROM koch.Prices WHERE Id=@Id", new { Id = id }) > 0;
        }
    }

    public class ProductPriceRepository : IProductPriceRepository
    {
        private readonly IDbConnectionFactory _factory;
        public ProductPriceRepository(IDbConnectionFactory factory) => _factory = factory;

        public async Task<List<ProductPrice>> GetByProductAsync(int productId)
        {
            using var conn = _factory.CreateConnection();
            return (await conn.QueryAsync<ProductPrice>(@"
                SELECT pp.*, pr.Name AS PriceName
                FROM koch.ProductPrices pp
                INNER JOIN koch.Prices pr ON pr.Id = pp.PriceId
                WHERE pp.ProductId = @ProductId
                ORDER BY pp.ValidFrom DESC, pp.Id DESC", new { ProductId = productId })).ToList();
        }

        public async Task<ProductPrice> GetByIdAsync(int id)
        {
            using var conn = _factory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<ProductPrice>(@"
                SELECT pp.*, pr.Name AS PriceName FROM koch.ProductPrices pp
                INNER JOIN koch.Prices pr ON pr.Id = pp.PriceId WHERE pp.Id=@Id", new { Id = id });
        }

        public async Task<ProductPrice> GetActiveAsync(int productId, int priceId)
        {
            using var conn = _factory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<ProductPrice>(@"
                SELECT TOP 1 pp.*, pr.Name AS PriceName FROM koch.ProductPrices pp
                INNER JOIN koch.Prices pr ON pr.Id = pp.PriceId
                WHERE pp.ProductId=@ProductId AND pp.PriceId=@PriceId AND pp.IsActive=1
                ORDER BY pp.ValidFrom DESC", new { ProductId = productId, PriceId = priceId });
        }

        public async Task<int> AddAsync(ProductPrice row)
        {
            using var conn = _factory.CreateConnection();
            return await conn.ExecuteScalarAsync<int>(@"
                INSERT INTO koch.ProductPrices (ProductId, PriceId, Amount, Currency, IsActive, ValidFrom, ValidTo, Notes, CreatedBy)
                OUTPUT INSERTED.Id
                VALUES (@ProductId, @PriceId, @Amount, @Currency, @IsActive, @ValidFrom, @ValidTo, @Notes, @CreatedBy)", row);
        }

        public async Task<bool> SetActiveAsync(int id, int productId, int priceId)
        {
            using var conn = _factory.CreateConnection();
            if (conn.State != ConnectionState.Open) conn.Open();
            using var tx = conn.BeginTransaction();
            await conn.ExecuteAsync(
                @"UPDATE koch.ProductPrices SET IsActive=0, ValidTo=SYSDATETIME()
                  WHERE ProductId=@ProductId AND PriceId=@PriceId AND IsActive=1 AND Id<>@Id",
                new { ProductId = productId, PriceId = priceId, Id = id }, tx);
            var n = await conn.ExecuteAsync(
                @"UPDATE koch.ProductPrices SET IsActive=1, ValidTo=NULL WHERE Id=@Id",
                new { Id = id }, tx);
            // sync product.Price cache for SALE price list if possible
            await conn.ExecuteAsync(@"
                UPDATE p SET p.Price = pp.Amount
                FROM koch.Products p
                INNER JOIN koch.ProductPrices pp ON pp.ProductId = p.Id AND pp.Id = @Id
                INNER JOIN koch.Prices pr ON pr.Id = pp.PriceId AND (pr.Code = N'SALE' OR pr.Name LIKE N'%فروش%')",
                new { Id = id }, tx);
            tx.Commit();
            return n > 0;
        }

        public async Task<bool> DeactivateAsync(int id)
        {
            using var conn = _factory.CreateConnection();
            return await conn.ExecuteAsync(
                "UPDATE koch.ProductPrices SET IsActive=0, ValidTo=SYSDATETIME() WHERE Id=@Id",
                new { Id = id }) > 0;
        }
    }

    public class InventoryStockRepository : IInventoryStockRepository
    {
        private readonly IDbConnectionFactory _factory;
        public InventoryStockRepository(IDbConnectionFactory factory) => _factory = factory;

        public async Task<List<InventoryStockItem>> GetListAsync(int? warehouseId = null, int? storeId = null, bool? lowStockOnly = null)
            => await GetListScopedAsync(DataScopeQuery.Unrestricted(), warehouseId, storeId, lowStockOnly);

        public async Task<List<InventoryStockItem>> GetListScopedAsync(DataScopeQuery scope, int? warehouseId = null, int? storeId = null, bool? lowStockOnly = null)
        {
            using var conn = _factory.CreateConnection();
            var scopeKind = scope?.Kind ?? "All";
            var scopeValues = scope?.Values != null && scope.Values.Count > 0
                ? string.Join(",", scope.Values)
                : null;

            var sql = $@"
                SELECT s.*, w.Name AS WarehouseName, w.StoreId, st.StoreName,
                       ISNULL(p.Name, s.ItemName) AS ProductName,
                       u.Name AS UnitName
                FROM koch.InventoryStock s
                INNER JOIN koch.Warehouses w ON w.Id = s.WarehouseId
                LEFT JOIN koch.Stores st ON st.StoreID = w.StoreId
                LEFT JOIN koch.Products p ON p.Id = s.ProductId
                LEFT JOIN koch.ProductUnits u ON u.Id = s.UnitId
                WHERE (@WarehouseId IS NULL OR s.WarehouseId = @WarehouseId)
                  AND (@StoreId IS NULL OR w.StoreId = @StoreId)
                  AND (@LowStockOnly IS NULL OR @LowStockOnly = 0 OR (s.MinQuantity IS NOT NULL AND s.Quantity <= s.MinQuantity))
                  AND {DataScopeSqlBuilder.StoreScopePredicate("st")}
                ORDER BY w.Name, ISNULL(p.Name, s.ItemName)";
            return (await conn.QueryAsync<InventoryStockItem>(sql, new
            {
                WarehouseId = warehouseId,
                StoreId = storeId,
                LowStockOnly = lowStockOnly,
                ScopeKind = scopeKind,
                ScopeValues = scopeValues,
                ActorUserId = scope?.ActorUserId
            })).ToList();
        }

        public async Task<InventoryStockItem> GetAsync(int warehouseId, int productId)
        {
            using var conn = _factory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<InventoryStockItem>(
                "SELECT * FROM koch.InventoryStock WHERE WarehouseId=@WarehouseId AND ProductId=@ProductId",
                new { WarehouseId = warehouseId, ProductId = productId });
        }

        public async Task EnsureStockRowAsync(int warehouseId, int productId, string itemName, string sku, int? unitId, decimal? minQty)
        {
            using var conn = _factory.CreateConnection();
            var exists = await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(1) FROM koch.InventoryStock WHERE WarehouseId=@WarehouseId AND ProductId=@ProductId",
                new { WarehouseId = warehouseId, ProductId = productId });
            if (exists > 0) return;
            await conn.ExecuteAsync(@"
                INSERT INTO koch.InventoryStock (WarehouseId, ProductId, Sku, ItemName, Quantity, ReservedQuantity, MinQuantity, UnitId, UpdatedAt)
                VALUES (@WarehouseId, @ProductId, @Sku, @ItemName, 0, 0, @MinQuantity, @UnitId, SYSDATETIME())",
                new { WarehouseId = warehouseId, ProductId = productId, Sku = sku, ItemName = itemName, MinQuantity = minQty, UnitId = unitId });
        }

        public async Task<bool> AdjustQuantityAsync(int warehouseId, int productId, decimal delta)
        {
            using var conn = _factory.CreateConnection();
            var n = await conn.ExecuteAsync(@"
                UPDATE koch.InventoryStock
                SET Quantity = Quantity + @Delta, LastMovementAt = SYSDATETIME(), UpdatedAt = SYSDATETIME()
                WHERE WarehouseId=@WarehouseId AND ProductId=@ProductId
                  AND (@Delta >= 0 OR Quantity + @Delta >= 0)",
                new { WarehouseId = warehouseId, ProductId = productId, Delta = delta });
            return n > 0;
        }

        public async Task<bool> SetMinQuantityAsync(int stockId, decimal? minQuantity)
        {
            using var conn = _factory.CreateConnection();
            return await conn.ExecuteAsync(
                "UPDATE koch.InventoryStock SET MinQuantity=@MinQuantity, UpdatedAt=SYSDATETIME() WHERE Id=@Id",
                new { Id = stockId, MinQuantity = minQuantity }) > 0;
        }

        public async Task<int> CountLowStockAsync(int? storeId = null)
        {
            using var conn = _factory.CreateConnection();
            return await conn.ExecuteScalarAsync<int>(@"
                SELECT COUNT(1) FROM koch.InventoryStock s
                INNER JOIN koch.Warehouses w ON w.Id = s.WarehouseId
                WHERE s.MinQuantity IS NOT NULL AND s.Quantity <= s.MinQuantity
                  AND (@StoreId IS NULL OR w.StoreId = @StoreId)", new { StoreId = storeId });
        }

        public async Task<int> CountLowStockScopedAsync(DataScopeQuery scope, int? storeId = null)
        {
            using var conn = _factory.CreateConnection();
            var scopeKind = scope?.Kind ?? "All";
            var scopeValues = scope?.Values != null && scope.Values.Count > 0
                ? string.Join(",", scope.Values) : null;
            return await conn.ExecuteScalarAsync<int>($@"
                SELECT COUNT(1) FROM koch.InventoryStock s
                INNER JOIN koch.Warehouses w ON w.Id = s.WarehouseId
                LEFT JOIN koch.Stores st ON st.StoreID = w.StoreId
                WHERE s.MinQuantity IS NOT NULL AND s.Quantity <= s.MinQuantity
                  AND (@StoreId IS NULL OR w.StoreId = @StoreId)
                  AND {DataScopeSqlBuilder.StoreScopePredicate("st")}",
                new { StoreId = storeId, ScopeKind = scopeKind, ScopeValues = scopeValues, ActorUserId = scope?.ActorUserId });
        }
    }

    public class InventoryDocumentRepository : IInventoryDocumentRepository
    {
        private readonly IDbConnectionFactory _factory;
        public InventoryDocumentRepository(IDbConnectionFactory factory) => _factory = factory;

        public Task<List<InventoryMovement>> GetListAsync(string type = null, int? warehouseId = null, int? storeId = null)
            => GetListScopedAsync(DataScopeQuery.Unrestricted(), type, warehouseId, storeId);

        public async Task<List<InventoryMovement>> GetListScopedAsync(DataScopeQuery scope, string type = null, int? warehouseId = null, int? storeId = null)
        {
            using var conn = _factory.CreateConnection();
            var scopeKind = scope?.Kind ?? "All";
            var scopeValues = scope?.Values != null && scope.Values.Count > 0
                ? string.Join(",", scope.Values) : null;
            return (await conn.QueryAsync<InventoryMovement>($@"
                SELECT m.*, w.Name AS WarehouseName, st.StoreName
                FROM koch.InventoryMovements m
                INNER JOIN koch.Warehouses w ON w.Id = m.WarehouseId
                LEFT JOIN koch.Stores st ON st.StoreID = ISNULL(m.StoreId, w.StoreId)
                WHERE (@Type IS NULL OR m.Type = @Type)
                  AND (@WarehouseId IS NULL OR m.WarehouseId = @WarehouseId)
                  AND (@StoreId IS NULL OR ISNULL(m.StoreId, w.StoreId) = @StoreId)
                  AND {DataScopeSqlBuilder.StoreScopePredicate("st")}
                ORDER BY m.MovementDate DESC, m.Id DESC",
                new { Type = type, WarehouseId = warehouseId, StoreId = storeId, ScopeKind = scopeKind, ScopeValues = scopeValues, ActorUserId = scope?.ActorUserId })).ToList();
        }

        public async Task<InventoryMovement> GetByIdAsync(int id)
        {
            using var conn = _factory.CreateConnection();
            var doc = await conn.QueryFirstOrDefaultAsync<InventoryMovement>(@"
                SELECT m.*, w.Name AS WarehouseName, st.StoreName
                FROM koch.InventoryMovements m
                INNER JOIN koch.Warehouses w ON w.Id = m.WarehouseId
                LEFT JOIN koch.Stores st ON st.StoreID = ISNULL(m.StoreId, w.StoreId)
                WHERE m.Id=@Id", new { Id = id });
            if (doc == null) return null;
            doc.Lines = (await conn.QueryAsync<InventoryMovementLine>(@"
                SELECT l.*, ISNULL(p.Name, l.ItemName) AS ProductName
                FROM koch.InventoryMovementLines l
                LEFT JOIN koch.Products p ON p.Id = l.ProductId
                WHERE l.MovementId=@Id", new { Id = id })).ToList();
            doc.TotalAmount = doc.Lines.Sum(x => x.LineTotal);
            return doc;
        }

        public async Task<string> GetNextDocumentNoAsync(string type)
        {
            using var conn = _factory.CreateConnection();
            var prefix = type switch
            {
                InventoryMovementTypes.Purchase => "PUR",
                InventoryMovementTypes.Sale => "SAL",
                InventoryMovementTypes.Adjust => "ADJ",
                _ => "DOC"
            };
            var n = await conn.ExecuteScalarAsync<int>(
                "SELECT ISNULL(COUNT(1),0)+1 FROM koch.InventoryMovements WHERE Type=@Type", new { Type = type });
            return $"{prefix}-{DateTime.Now:yyyyMMdd}-{n:D4}";
        }

        public async Task<int> CreateDraftAsync(InventoryMovement doc)
        {
            using var conn = _factory.CreateConnection();
            if (string.IsNullOrWhiteSpace(doc.DocumentNo))
                doc.DocumentNo = await GetNextDocumentNoAsync(doc.Type);
            doc.Status ??= InventoryMovementStatuses.Draft;
            var id = await conn.ExecuteScalarAsync<int>(@"
                INSERT INTO koch.InventoryMovements
                    (WarehouseId, StoreId, Type, DocumentNo, ReferenceNo, MovementDate, Description, TargetWarehouseId, CreatedBy, Status)
                OUTPUT INSERTED.Id
                VALUES (@WarehouseId, @StoreId, @Type, @DocumentNo, @ReferenceNo, @MovementDate, @Description, @TargetWarehouseId, @CreatedBy, @Status)",
                doc);
            if (doc.Lines?.Count > 0)
                await ReplaceLinesAsync(id, doc.Lines);
            return id;
        }

        public async Task<bool> UpdateDraftAsync(InventoryMovement doc)
        {
            using var conn = _factory.CreateConnection();
            var status = await conn.ExecuteScalarAsync<string>("SELECT Status FROM koch.InventoryMovements WHERE Id=@Id", new { doc.Id });
            if (!string.Equals(status, InventoryMovementStatuses.Draft, StringComparison.OrdinalIgnoreCase))
                return false;
            await conn.ExecuteAsync(@"
                UPDATE koch.InventoryMovements SET
                    WarehouseId=@WarehouseId, StoreId=@StoreId, ReferenceNo=@ReferenceNo,
                    MovementDate=@MovementDate, Description=@Description, TargetWarehouseId=@TargetWarehouseId
                WHERE Id=@Id AND Status=N'Draft'", doc);
            await ReplaceLinesAsync(doc.Id, doc.Lines ?? new List<InventoryMovementLine>());
            return true;
        }

        public async Task ReplaceLinesAsync(int movementId, List<InventoryMovementLine> lines)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync("DELETE FROM koch.InventoryMovementLines WHERE MovementId=@Id", new { Id = movementId });
            foreach (var line in lines ?? new List<InventoryMovementLine>())
            {
                line.MovementId = movementId;
                await conn.ExecuteAsync(@"
                    INSERT INTO koch.InventoryMovementLines
                        (MovementId, ProductId, Sku, ItemName, Quantity, Unit, UnitId, UnitPrice, BatchNo, ExpiryDate, Notes)
                    VALUES (@MovementId, @ProductId, @Sku, @ItemName, @Quantity, @Unit, @UnitId, @UnitPrice, @BatchNo, @ExpiryDate, @Notes)",
                    line);
            }
        }

        public async Task<bool> PostAsync(int movementId, int postedBy)
        {
            using var conn = _factory.CreateConnection();
            if (conn.State != ConnectionState.Open) conn.Open();
            using var tx = conn.BeginTransaction();

            var doc = await conn.QueryFirstOrDefaultAsync<InventoryMovement>(
                "SELECT * FROM koch.InventoryMovements WHERE Id=@Id", new { Id = movementId }, tx);
            if (doc == null || !string.Equals(doc.Status, InventoryMovementStatuses.Draft, StringComparison.OrdinalIgnoreCase))
            {
                tx.Rollback();
                return false;
            }

            var lines = (await conn.QueryAsync<InventoryMovementLine>(
                "SELECT * FROM koch.InventoryMovementLines WHERE MovementId=@Id", new { Id = movementId }, tx)).ToList();
            if (lines.Count == 0)
            {
                tx.Rollback();
                return false;
            }

            foreach (var line in lines)
            {
                if (line.ProductId == null || line.ProductId <= 0) continue;
                var productId = line.ProductId.Value;

                var exists = await conn.ExecuteScalarAsync<int>(
                    "SELECT COUNT(1) FROM koch.InventoryStock WHERE WarehouseId=@W AND ProductId=@P",
                    new { W = doc.WarehouseId, P = productId }, tx);
                if (exists == 0)
                {
                    await conn.ExecuteAsync(@"
                        INSERT INTO koch.InventoryStock (WarehouseId, ProductId, Sku, ItemName, Quantity, ReservedQuantity, UnitId, Unit, UpdatedAt)
                        VALUES (@W, @P, @Sku, @Name, 0, 0, @UnitId, @Unit, SYSDATETIME())",
                        new { W = doc.WarehouseId, P = productId, Sku = line.Sku, Name = line.ItemName, line.UnitId, line.Unit }, tx);
                }

                decimal delta = doc.Type switch
                {
                    InventoryMovementTypes.Purchase => line.Quantity,
                    InventoryMovementTypes.Sale => -line.Quantity,
                    InventoryMovementTypes.Adjust => line.Quantity, // signed quantity for adjust
                    _ => 0
                };

                if (delta == 0) continue;

                if (delta < 0)
                {
                    var qty = await conn.ExecuteScalarAsync<decimal>(
                        "SELECT Quantity FROM koch.InventoryStock WHERE WarehouseId=@W AND ProductId=@P",
                        new { W = doc.WarehouseId, P = productId }, tx);
                    if (qty + delta < 0)
                    {
                        tx.Rollback();
                        throw new InvalidOperationException($"موجودی کافی نیست برای محصول {line.ItemName ?? productId.ToString()}");
                    }
                }

                await conn.ExecuteAsync(@"
                    UPDATE koch.InventoryStock
                    SET Quantity = Quantity + @Delta, LastMovementAt = SYSDATETIME(), UpdatedAt = SYSDATETIME()
                    WHERE WarehouseId=@W AND ProductId=@P",
                    new { W = doc.WarehouseId, P = productId, Delta = delta }, tx);
            }

            await conn.ExecuteAsync(@"
                UPDATE koch.InventoryMovements
                SET Status=N'Posted', PostedAt=SYSDATETIME(), PostedBy=@PostedBy
                WHERE Id=@Id", new { Id = movementId, PostedBy = postedBy }, tx);

            tx.Commit();
            return true;
        }

        public async Task<bool> DeleteDraftAsync(int id)
        {
            using var conn = _factory.CreateConnection();
            return await conn.ExecuteAsync(
                "DELETE FROM koch.InventoryMovements WHERE Id=@Id AND Status=N'Draft'", new { Id = id }) > 0;
        }
    }

    public class StoreOwnerAccessRepository : IStoreOwnerAccessRepository
    {
        private readonly IDbConnectionFactory _factory;
        public StoreOwnerAccessRepository(IDbConnectionFactory factory) => _factory = factory;

        public async Task<List<StoreOwnerModuleAccess>> GetByUserAsync(int userId)
        {
            using var conn = _factory.CreateConnection();
            return (await conn.QueryAsync<StoreOwnerModuleAccess>(
                "SELECT * FROM koch.StoreOwnerModuleAccess WHERE UserId=@UserId",
                new { UserId = userId })).ToList();
        }

        public async Task UpsertAsync(int userId, string moduleKey, bool canAccess)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(@"
                IF EXISTS (SELECT 1 FROM koch.StoreOwnerModuleAccess WHERE UserId=@UserId AND ModuleKey=@ModuleKey)
                    UPDATE koch.StoreOwnerModuleAccess SET CanAccess=@CanAccess, UpdatedAt=SYSDATETIME()
                    WHERE UserId=@UserId AND ModuleKey=@ModuleKey
                ELSE
                    INSERT INTO koch.StoreOwnerModuleAccess (UserId, ModuleKey, CanAccess)
                    VALUES (@UserId, @ModuleKey, @CanAccess)",
                new { UserId = userId, ModuleKey = moduleKey, CanAccess = canAccess });
        }

        public async Task UpsertManyAsync(int userId, IEnumerable<StoreOwnerModuleAccess> items)
        {
            foreach (var item in items)
                await UpsertAsync(userId, item.ModuleKey, item.CanAccess);
        }
    }
}
