using System;

namespace MyShop.Core.Models
{
    public enum TargetAudience
    {
        ShopOwner = 1,
        Customer = 2,
        Both = 3
    }

    public class Announcement
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string AlertColor { get; set; } = "Info"; // Info, Success, Warning, Danger, or HEX color
        public DateTime StartDateTime { get; set; }
        public DateTime EndDateTime { get; set; }
        public TargetAudience TargetAudience { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int CreatedBy { get; set; }
    }
}
