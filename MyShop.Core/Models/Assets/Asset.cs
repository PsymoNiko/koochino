using System;

namespace MyShop.Core.Models.Assets
{
    public class Asset
    {
        public int Id { get; set; }
        public int? CompanyId { get; set; }
        public int? StoreId { get; set; }
        public int? CategoryId { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime? PurchaseDate { get; set; }
        public decimal? PurchasePrice { get; set; }
        public decimal? CurrentValue { get; set; }
        public string Location { get; set; }
        public int? AssignedTo { get; set; }
        public int? PersonnelId { get; set; }
        public string Status { get; set; }
        public DateTime? WarrantyEndDate { get; set; }
        public decimal? DepreciationRate { get; set; }
        public string ImageUrl { get; set; }
        public string SerialNo { get; set; }
        public string Notes { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
