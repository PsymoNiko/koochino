using System;

namespace MyShop.Core.Models.Users
{
    /// <summary>
    /// درخواست احراز هویت مغازه‌دار (کد ملی، نام پدر، کارت ملی، سلفی)
    /// </summary>
    public class UserIdentityVerification
    {
        public int VerificationId { get; set; }
        public int UserId { get; set; }

        public string NationalCode { get; set; }
        public string FatherName { get; set; }
        public DateTime? BirthDate { get; set; }
        public string PhoneNumber { get; set; }

        public string NationalCardImageUrl { get; set; }
        public string SelfieImageUrl { get; set; }

        /// <summary>کدی که کاربر باید روی برگه دست‌نویس بنویسد و در عکس سلفی نشان دهد</summary>
        public string VerificationCode { get; set; }

        /// <summary>کد پیگیری که بعد از ثبت نهایی به کاربر نمایش داده می‌شود</summary>
        public string TrackingCode { get; set; }

        /// <summary>Pending / Approved / Rejected</summary>
        public string Status { get; set; } = "Pending";
        public string RejectionReason { get; set; }

        public DateTime SubmittedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public int? ReviewedBy { get; set; }

        /// <summary>فروشگاهی که در مرحله «تعریف مغازه» ویزارد ساخته شده</summary>
        public int? StoreId { get; set; }

        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Username { get; set; }
    }
}
