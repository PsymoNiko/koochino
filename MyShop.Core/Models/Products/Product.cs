using System;

namespace MyShop.Core.Models.Products
{
    public class Product
    {
        public int Id { get; set; }
        public int? CompanyId { get; set; }
        public int? StoreId { get; set; }
        public int? CategoryId { get; set; }
        public int? UnitId { get; set; }
        public string Name { get; set; }
        public string Slug { get; set; }
        public string Description { get; set; }
        public string ShortDescription { get; set; }
        public decimal? Price { get; set; }
        public decimal? CompareAtPrice { get; set; }
        public decimal? Cost { get; set; }
        public string Sku { get; set; }
        public string Barcode { get; set; }
        public int Quantity { get; set; }
        public bool IsActive { get; set; }
        public bool IsFeatured { get; set; }
        public string ImageUrl { get; set; }
        public int SortOrder { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
