using MyShop.Application.Interfaces;
using MyShop.Application.Interfaces.Inventory;
using MyShop.Application.Interfaces.Security;
using MyShop.Core.Models.Inventory;
using MyShop.Core.Models.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Application.Services.Inventory
{
    public class ProductUnitService : IProductUnitService
    {
        private readonly IProductUnitRepository _repo;
        public ProductUnitService(IProductUnitRepository repo) => _repo = repo;
        public Task<List<ProductUnit>> GetAllAsync(bool? onlyActive = null) => _repo.GetAllAsync(onlyActive);
        public Task<int> SaveAsync(ProductUnit unit) => _repo.UpsertAsync(unit);
        public Task<bool> DeleteAsync(int id) => _repo.DeleteAsync(id);
    }

    public class PriceListService : IPriceListService
    {
        private readonly IPriceListRepository _repo;
        public PriceListService(IPriceListRepository repo) => _repo = repo;
        public Task<List<PriceList>> GetAllAsync(bool? onlyActive = null) => _repo.GetAllAsync(onlyActive);
        public Task<int> SaveAsync(PriceList price) => _repo.UpsertAsync(price);
        public Task<bool> DeleteAsync(int id) => _repo.DeleteAsync(id);
    }

    public class ProductPriceService : IProductPriceService
    {
        private readonly IProductPriceRepository _repo;
        public ProductPriceService(IProductPriceRepository repo) => _repo = repo;
        public Task<List<ProductPrice>> GetByProductAsync(int productId) => _repo.GetByProductAsync(productId);

        public async Task<(bool Ok, string Message)> AddPriceAsync(ProductPrice row, bool activate)
        {
            if (row.ProductId <= 0 || row.PriceId <= 0 || row.Amount < 0)
                return (false, "اطلاعات قیمت نامعتبر است.");
            if (row.ValidFrom == default) row.ValidFrom = DateTime.Now;
            row.IsActive = false;
            var id = await _repo.AddAsync(row);
            if (id <= 0) return (false, "خطا در ثبت قیمت.");
            if (activate)
            {
                await _repo.SetActiveAsync(id, row.ProductId, row.PriceId);
                return (true, "قیمت ثبت و فعال شد.");
            }
            return (true, "قیمت ثبت شد.");
        }

        public async Task<(bool Ok, string Message)> ActivateAsync(int id)
        {
            var row = await _repo.GetByIdAsync(id);
            if (row == null) return (false, "قیمت یافت نشد.");
            var ok = await _repo.SetActiveAsync(id, row.ProductId, row.PriceId);
            return ok ? (true, "قیمت فعال شد.") : (false, "خطا در فعال‌سازی.");
        }
    }

    public class InventoryStockService : IInventoryStockService
    {
        private readonly IInventoryStockRepository _repo;
        private readonly IDataScopeResolver _scope;

        public InventoryStockService(IInventoryStockRepository repo, IDataScopeResolver scope = null)
        {
            _repo = repo;
            _scope = scope;
        }

        public Task<List<InventoryStockItem>> GetListAsync(int? warehouseId = null, int? storeId = null, bool? lowStockOnly = null)
            => _repo.GetListAsync(warehouseId, storeId, lowStockOnly);

        public async Task<List<InventoryStockItem>> GetListForUserAsync(int userId, int? warehouseId = null, int? storeId = null, bool? lowStockOnly = null, int? siteId = null)
        {
            if (_scope == null || userId <= 0)
                return await _repo.GetListAsync(warehouseId, storeId, lowStockOnly);
            var query = await _scope.BuildQueryAsync(userId, "Inventory.View", siteId);
            return await _repo.GetListScopedAsync(query, warehouseId, storeId, lowStockOnly);
        }

        public Task<bool> SetMinQuantityAsync(int stockId, decimal? minQuantity) => _repo.SetMinQuantityAsync(stockId, minQuantity);

        public Task<int> CountLowStockAsync(int? storeId = null) => _repo.CountLowStockAsync(storeId);

        public async Task<int> CountLowStockForUserAsync(int userId, int? storeId = null, int? siteId = null)
        {
            if (_scope == null || userId <= 0)
                return await _repo.CountLowStockAsync(storeId);
            var query = await _scope.BuildQueryAsync(userId, "Inventory.View", siteId);
            return await _repo.CountLowStockScopedAsync(query, storeId);
        }
    }

    public class InventoryDocumentService : IInventoryDocumentService
    {
        private readonly IInventoryDocumentRepository _repo;
        private readonly IInventoryStockRepository _stock;
        private readonly IStoreService _stores;
        private readonly IRequestAuditContext _audit;
        private readonly IDataScopeResolver _scope;

        public InventoryDocumentService(
            IInventoryDocumentRepository repo,
            IInventoryStockRepository stock,
            IStoreService stores = null,
            IRequestAuditContext audit = null,
            IDataScopeResolver scope = null)
        {
            _repo = repo;
            _stock = stock;
            _stores = stores;
            _audit = audit;
            _scope = scope;
        }

        public Task<List<InventoryMovement>> GetListAsync(string type = null, int? warehouseId = null, int? storeId = null)
            => _repo.GetListAsync(type, warehouseId, storeId);

        public async Task<List<InventoryMovement>> GetListForUserAsync(int userId, string type = null, int? warehouseId = null, int? storeId = null, int? siteId = null)
        {
            if (_scope == null || userId <= 0)
                return await _repo.GetListAsync(type, warehouseId, storeId);
            var query = await _scope.BuildQueryAsync(userId, "Inventory.View", siteId);
            return await _repo.GetListScopedAsync(query, type, warehouseId, storeId);
        }

        public Task<InventoryMovement> GetByIdAsync(int id) => _repo.GetByIdAsync(id);

        public async Task<(bool Ok, string Message, int Id)> SaveDraftAsync(InventoryMovement doc)
        {
            if (doc.WarehouseId <= 0) return (false, "انبار را انتخاب کنید.", 0);
            if (string.IsNullOrWhiteSpace(doc.Type)) return (false, "نوع سند نامعتبر است.", 0);
            if (doc.Lines == null || doc.Lines.Count == 0) return (false, "حداقل یک قلم کالا لازم است.", 0);
            if (doc.MovementDate == default) doc.MovementDate = DateTime.Today;
            doc.Status = InventoryMovementStatuses.Draft;

            foreach (var line in doc.Lines)
            {
                if (line.ProductId == null || line.ProductId <= 0)
                    return (false, "محصول را برای همه ردیف‌ها انتخاب کنید.", 0);
                if (line.Quantity == 0)
                    return (false, "مقدار نمی‌تواند صفر باشد.", 0);
            }

            try
            {
                if (doc.Id > 0)
                {
                    var ok = await _repo.UpdateDraftAsync(doc);
                    return ok ? (true, "پیش‌نویس ذخیره شد.", doc.Id) : (false, "فقط پیش‌نویس قابل ویرایش است.", 0);
                }
                var id = await _repo.CreateDraftAsync(doc);
                return id > 0 ? (true, "پیش‌نویس ایجاد شد.", id) : (false, "خطا در ایجاد سند.", 0);
            }
            catch (Exception ex)
            {
                return (false, ex.Message, 0);
            }
        }

        public async Task<(bool Ok, string Message)> PostAsync(int movementId, int postedBy)
        {
            try
            {
                var doc = await _repo.GetByIdAsync(movementId);
                if (doc == null)
                    return (false, "سند یافت نشد.");
                if (!await CanManageDocumentAsync(doc, postedBy))
                    return (false, "اجازه ثبت این سند را ندارید.");
                var ok = await _repo.PostAsync(movementId, postedBy);
                return ok ? (true, "سند با موفقیت ثبت قطعی شد و موجودی به‌روز گردید.") : (false, "ثبت قطعی ممکن نیست (پیش‌نویس نامعتبر).");
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        async Task<bool> CanManageDocumentAsync(InventoryMovement doc, int userId)
        {
            if (_audit?.IsAdmin == true) return true;
            if (userId <= 0) return false;
            if (doc.CreatedBy.HasValue && doc.CreatedBy.Value == userId) return true;
            if (_stores == null || !doc.StoreId.HasValue) return userId == (_audit?.UserId ?? userId);
            var store = await _stores.GetByIdAsync(doc.StoreId.Value);
            return store != null && store.UserID == userId;
        }

        public async Task<(bool Ok, string Message)> DeleteDraftAsync(int id)
        {
            var ok = await _repo.DeleteDraftAsync(id);
            return ok ? (true, "حذف شد.") : (false, "فقط پیش‌نویس قابل حذف است.");
        }
    }

    public class StoreOwnerPermissionService : IStoreOwnerPermissionService
    {
        private readonly IStoreOwnerAccessRepository _repo;
        private readonly IPermissionService _engine;

        public StoreOwnerPermissionService(IStoreOwnerAccessRepository repo, IPermissionService engine = null)
        {
            _repo = repo;
            _engine = engine;
        }

        public async Task<bool> HasAsync(int userId, string moduleKey)
        {
            if (_engine != null && await _engine.IsEngineEnabledAsync())
            {
                var code = PortalPermissionCodes.StoreOwnerModule(moduleKey);
                return await _engine.HasPermissionAsync(userId, code);
            }

            var map = await GetLegacyMapAsync(userId);
            return map.TryGetValue(moduleKey, out var v) && v;
        }

        public async Task<Dictionary<string, bool>> GetMapAsync(int userId)
        {
            if (_engine != null && await _engine.IsEngineEnabledAsync())
            {
                var dict = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
                foreach (var key in StoreOwnerModules.All)
                {
                    var code = PortalPermissionCodes.StoreOwnerModule(key);
                    dict[key] = await _engine.HasPermissionAsync(userId, code);
                }
                return dict;
            }

            return await GetLegacyMapAsync(userId);
        }

        async Task<Dictionary<string, bool>> GetLegacyMapAsync(int userId)
        {
            var rows = await _repo.GetByUserAsync(userId);
            var dict = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            foreach (var key in StoreOwnerModules.All)
            {
                var row = rows.FirstOrDefault(r => string.Equals(r.ModuleKey, key, StringComparison.OrdinalIgnoreCase));
                dict[key] = row != null ? row.CanAccess : StoreOwnerModules.DefaultAccess(key);
            }
            return dict;
        }

        public async Task SaveMapAsync(int userId, Dictionary<string, bool> map)
        {
            var items = map.Select(kv => new StoreOwnerModuleAccess
            {
                UserId = userId,
                ModuleKey = kv.Key,
                CanAccess = kv.Value
            });
            await _repo.UpsertManyAsync(userId, items);
        }
    }
}
