using System;

namespace MyShop.Core.Models.Dto
{
    /// <summary>
    /// DTO برای نمایش تراکنش‌های امتیاز
    /// </summary>
    public class PointTransactionDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string UserPhoneNumber { get; set; }
        public int? MissionId { get; set; }
        public string MissionTitle { get; set; }
        public string MissionType { get; set; }
        public int Points { get; set; }
        public string Status { get; set; }
        public string StatusText { get; set; } // متن فارسی وضعیت
        public string Reason { get; set; }
        public string ReferenceId { get; set; }
        public string ReferenceType { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string ReviewerName { get; set; }
        public string ReviewNotes { get; set; }
    }
}
