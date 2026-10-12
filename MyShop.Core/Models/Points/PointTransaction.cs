using System;

namespace MyShop.Core.Models
{
    /// <summary>
    /// لاگ تمام تراکنش‌های امتیاز
    /// </summary>
    public class PointTransaction
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int? MissionId { get; set; } // Nullable برای امتیازهای دستی
        public string MissionType { get; set; } // "PurchaseRating", "StoreComment", "PurchaseAmount", "Manual", "Custom"
        public int Points { get; set; }
        public string Status { get; set; } // "Pending", "Approved", "Rejected"
        public string Reason { get; set; } // علت دریافت امتیاز
        public string ReferenceId { get; set; } // ID مرجع (مثلاً OrderId, CommentId)
        public string ReferenceType { get; set; } // نوع مرجع (مثلاً "Order", "Comment")
        public DateTime CreatedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public int? ReviewedByUserId { get; set; }
        public string ReviewNotes { get; set; }
        
        // Navigation properties (برای نمایش)
        public string UserName { get; set; }
        public string UserPhoneNumber { get; set; }
        public string MissionTitle { get; set; }
        public string ReviewerName { get; set; }
    }
}
