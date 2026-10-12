using System;

namespace MyShop.Core.Models
{
    /// <summary>
    /// امتیاز فعلی هر کاربر (برای performance)
    /// </summary>
    public class UserPoints
    {
        public int UserId { get; set; }
        public int TotalPoints { get; set; }
        public int AvailablePoints { get; set; } // امتیازهای تایید شده
        public int PendingPoints { get; set; } // امتیازهای در انتظار تایید
        public DateTime LastUpdatedAt { get; set; }
        
        // Navigation properties
        public string UserName { get; set; }
        public string UserPhoneNumber { get; set; }
    }
}
