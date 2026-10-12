using System;
using System.Collections.Generic;

namespace MyShop.Core.Models.Inventory
{
    public class ProductUnit
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Symbol { get; set; }
        public bool IsActive { get; set; } = true;
        public int SortOrder { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class PriceList
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; } = true;
        public int SortOrder { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ProductPrice
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public int PriceId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public bool IsActive { get; set; }
        public DateTime ValidFrom { get; set; }
        public DateTime? ValidTo { get; set; }
        public string Notes { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        // display
        public string PriceName { get; set; }
        public string ProductName { get; set; }
    }

    public class InventoryStockItem
    {
        public int Id { get; set; }
        public int WarehouseId { get; set; }
        public int? ProductId { get; set; }
        public string Sku { get; set; }
        public string ItemName { get; set; }
        public decimal Quantity { get; set; }
        public decimal ReservedQuantity { get; set; }
        public decimal? MinQuantity { get; set; }
        public decimal? MaxQuantity { get; set; }
        public string Unit { get; set; }
        public int? UnitId { get; set; }
        public string LocationInWarehouse { get; set; }
        public DateTime? LastMovementAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        // display
        public string WarehouseName { get; set; }
        public string StoreName { get; set; }
        public int? StoreId { get; set; }
        public string ProductName { get; set; }
        public string UnitName { get; set; }
        public bool IsLowStock => MinQuantity.HasValue && Quantity <= MinQuantity.Value;
    }

    public static class InventoryMovementTypes
    {
        public const string Purchase = "Purchase";
        public const string Sale = "Sale";
        public const string Adjust = "Adjust";
        public const string Transfer = "Transfer";
    }

    public static class InventoryMovementStatuses
    {
        public const string Draft = "Draft";
        public const string Posted = "Posted";
        public const string Cancelled = "Cancelled";
    }

    public class InventoryMovement
    {
        public int Id { get; set; }
        public int WarehouseId { get; set; }
        public int? StoreId { get; set; }
        public string Type { get; set; }
        public string DocumentNo { get; set; }
        public string ReferenceNo { get; set; }
        public DateTime MovementDate { get; set; }
        public string Description { get; set; }
        public int? TargetWarehouseId { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; }
        public DateTime? PostedAt { get; set; }
        public int? PostedBy { get; set; }
        // display
        public string WarehouseName { get; set; }
        public string StoreName { get; set; }
        public List<InventoryMovementLine> Lines { get; set; } = new();
        public decimal TotalAmount { get; set; }
    }

    public class InventoryMovementLine
    {
        public int Id { get; set; }
        public int MovementId { get; set; }
        public int? ProductId { get; set; }
        public string Sku { get; set; }
        public string ItemName { get; set; }
        public decimal Quantity { get; set; }
        public string Unit { get; set; }
        public int? UnitId { get; set; }
        public decimal? UnitPrice { get; set; }
        public string BatchNo { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string Notes { get; set; }
        public string ProductName { get; set; }
        public decimal LineTotal => Quantity * (UnitPrice ?? 0);
    }

    public static class StoreOwnerModules
    {
        public const string Inventory = "Inventory";
        public const string Products = "Products";
        public const string Orders = "Orders";
        public const string Coupons = "Coupons";
        public const string ChanceBox = "ChanceBox";
        public const string Documents = "Documents";
        public const string Stores = "Stores";
        public const string Commerce = "Commerce";
        public const string Sms = "Sms";

        public static readonly string[] All =
        {
            Stores, Products, Orders, Coupons, ChanceBox, Documents, Inventory, Commerce, Sms
        };

        public static readonly Dictionary<string, string> Labels = new(StringComparer.OrdinalIgnoreCase)
        {
            [Stores] = "فروشگاه‌ها",
            [Products] = "محصولات",
            [Orders] = "سفارشات",
            [Coupons] = "کوپن‌ها",
            [ChanceBox] = "جعبه‌شانس",
            [Documents] = "مدارک",
            [Inventory] = "انبار و موجودی",
            [Commerce] = "فروشگاه آنلاین",
            [Sms] = "پیامک"
        };

        /// <summary>بدون ردیف صریح در StoreOwnerModuleAccess، دسترسی رد می‌شود (Deny by default).
        /// پیش از این متد برای همه‌ی ماژول‌ها به‌جز Inventory پیش‌فرض Allow بود؛ بعد از backfill
        /// ردیف‌های صریح برای همه‌ی فروشگاه‌داران موجود (سند فاز ۰، آیتم ۳)، اکنون Deny پیش‌فرض است.</summary>
        public static bool DefaultAccess(string moduleKey) => false;
    }

    public class StoreOwnerModuleAccess
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string ModuleKey { get; set; }
        public bool CanAccess { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
