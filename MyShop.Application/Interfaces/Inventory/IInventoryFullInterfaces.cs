using MyShop.Core.Models.Inventory;
using MyShop.Core.Models.Security;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Interfaces.Inventory
{
    public interface IProductUnitRepository
    {
        Task<List<ProductUnit>> GetAllAsync(bool? onlyActive = null);
        Task<ProductUnit> GetByIdAsync(int id);
        Task<int> UpsertAsync(ProductUnit unit);
        Task<bool> DeleteAsync(int id);
    }

    public interface IPriceListRepository
    {
        Task<List<PriceList>> GetAllAsync(bool? onlyActive = null);
        Task<PriceList> GetByIdAsync(int id);
        Task<int> UpsertAsync(PriceList price);
        Task<bool> DeleteAsync(int id);
    }

    public interface IProductPriceRepository
    {
        Task<List<ProductPrice>> GetByProductAsync(int productId);
        Task<ProductPrice> GetByIdAsync(int id);
        Task<ProductPrice> GetActiveAsync(int productId, int priceId);
        Task<int> AddAsync(ProductPrice row);
        Task<bool> SetActiveAsync(int id, int productId, int priceId);
        Task<bool> DeactivateAsync(int id);
    }

    public interface IInventoryStockRepository
    {
        Task<List<InventoryStockItem>> GetListAsync(int? warehouseId = null, int? storeId = null, bool? lowStockOnly = null);
        Task<List<InventoryStockItem>> GetListScopedAsync(DataScopeQuery scope, int? warehouseId = null, int? storeId = null, bool? lowStockOnly = null);
        Task<InventoryStockItem> GetAsync(int warehouseId, int productId);
        Task EnsureStockRowAsync(int warehouseId, int productId, string itemName, string sku, int? unitId, decimal? minQty);
        Task<bool> AdjustQuantityAsync(int warehouseId, int productId, decimal delta);
        Task<bool> SetMinQuantityAsync(int stockId, decimal? minQuantity);
        Task<int> CountLowStockAsync(int? storeId = null);
        Task<int> CountLowStockScopedAsync(DataScopeQuery scope, int? storeId = null);
    }

    public interface IInventoryDocumentRepository
    {
        Task<List<InventoryMovement>> GetListAsync(string type = null, int? warehouseId = null, int? storeId = null);
        Task<List<InventoryMovement>> GetListScopedAsync(MyShop.Core.Models.Security.DataScopeQuery scope, string type = null, int? warehouseId = null, int? storeId = null);
        Task<InventoryMovement> GetByIdAsync(int id);
        Task<string> GetNextDocumentNoAsync(string type);
        Task<int> CreateDraftAsync(InventoryMovement doc);
        Task<bool> UpdateDraftAsync(InventoryMovement doc);
        Task ReplaceLinesAsync(int movementId, List<InventoryMovementLine> lines);
        Task<bool> PostAsync(int movementId, int postedBy);
        Task<bool> DeleteDraftAsync(int id);
    }

    public interface IStoreOwnerAccessRepository
    {
        Task<List<StoreOwnerModuleAccess>> GetByUserAsync(int userId);
        Task UpsertAsync(int userId, string moduleKey, bool canAccess);
        Task UpsertManyAsync(int userId, IEnumerable<StoreOwnerModuleAccess> items);
    }

    public interface IProductUnitService
    {
        Task<List<ProductUnit>> GetAllAsync(bool? onlyActive = null);
        Task<int> SaveAsync(ProductUnit unit);
        Task<bool> DeleteAsync(int id);
    }

    public interface IPriceListService
    {
        Task<List<PriceList>> GetAllAsync(bool? onlyActive = null);
        Task<int> SaveAsync(PriceList price);
        Task<bool> DeleteAsync(int id);
    }

    public interface IProductPriceService
    {
        Task<List<ProductPrice>> GetByProductAsync(int productId);
        Task<(bool Ok, string Message)> AddPriceAsync(ProductPrice row, bool activate);
        Task<(bool Ok, string Message)> ActivateAsync(int id);
    }

    public interface IInventoryStockService
    {
        Task<List<InventoryStockItem>> GetListAsync(int? warehouseId = null, int? storeId = null, bool? lowStockOnly = null);
        Task<List<InventoryStockItem>> GetListForUserAsync(int userId, int? warehouseId = null, int? storeId = null, bool? lowStockOnly = null, int? siteId = null);
        Task<bool> SetMinQuantityAsync(int stockId, decimal? minQuantity);
        Task<int> CountLowStockAsync(int? storeId = null);
        Task<int> CountLowStockForUserAsync(int userId, int? storeId = null, int? siteId = null);
    }

    public interface IInventoryDocumentService
    {
        Task<List<InventoryMovement>> GetListAsync(string type = null, int? warehouseId = null, int? storeId = null);
        Task<List<InventoryMovement>> GetListForUserAsync(int userId, string type = null, int? warehouseId = null, int? storeId = null, int? siteId = null);
        Task<InventoryMovement> GetByIdAsync(int id);
        Task<(bool Ok, string Message, int Id)> SaveDraftAsync(InventoryMovement doc);
        Task<(bool Ok, string Message)> PostAsync(int movementId, int postedBy);
        Task<(bool Ok, string Message)> DeleteDraftAsync(int id);
    }

    public interface IStoreOwnerPermissionService
    {
        Task<bool> HasAsync(int userId, string moduleKey);
        Task<Dictionary<string, bool>> GetMapAsync(int userId);
        Task SaveMapAsync(int userId, Dictionary<string, bool> map);
    }
}
