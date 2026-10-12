using System;

namespace MyShop.Core.Models.Users
{
    /// <summary>یک مدرک آپلودشده (کارت ملی یا سلفی) در فرآیند احراز هویت مغازه‌دار</summary>
    public class UserIdentityDocument
    {
        public int DocumentId { get; set; }
        public int UserId { get; set; }
        public int? VerificationId { get; set; }

        /// <summary>NationalCard / Selfie</summary>
        public string DocumentType { get; set; }
        public string ImageUrl { get; set; }

        /// <summary>فقط برای نوع Selfie پر می‌شود</summary>
        public string VerificationCode { get; set; }

        /// <summary>Pending / Approved / Rejected</summary>
        public string Status { get; set; } = "Pending";
        public string RejectionReason { get; set; }

        public DateTime UploadedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public int? ReviewedBy { get; set; }

        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Username { get; set; }
    }
}
