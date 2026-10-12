using System;

namespace MyShop.Core.Models.Contact
{
    public class ContactLead
    {
        public int Id { get; set; }
        public int? SiteId { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string Source { get; set; } = "landing";
        public bool IsRead { get; set; }
        public string Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? ConvertedContactId { get; set; }
        public DateTime? ConvertedAt { get; set; }
        public string SiteName { get; set; }
    }
}
