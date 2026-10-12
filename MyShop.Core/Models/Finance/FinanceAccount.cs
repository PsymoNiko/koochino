using System;

namespace MyShop.Core.Models.Finance
{
    public class FinanceAccount
    {
        public int Id { get; set; }
        public int? CompanyId { get; set; }
        public int? StoreId { get; set; }
        public int? ParentId { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public decimal Balance { get; set; }
        public string Currency { get; set; }
        public bool IsActive { get; set; }
        public int SortOrder { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
