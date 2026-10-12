using System;

namespace MyShop.Core.Models.ProjectManagement
{
    public class Project
    {
        public int Id { get; set; }
        public int? CompanyId { get; set; }
        public int? StoreId { get; set; }
        public int? SiteId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public string Priority { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public byte Progress { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
