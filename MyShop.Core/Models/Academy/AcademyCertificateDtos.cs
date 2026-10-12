using System;

namespace MyShop.Core.Models.Academy
{
    public class AcademyCertificateDto
    {
        public int Id { get; set; }
        public int EnrollmentId { get; set; }
        public int StudentId { get; set; }
        public int CourseId { get; set; }
        public string CertificateNo { get; set; }
        public string Status { get; set; }
        public DateTime IssuedAt { get; set; }
        public int? IssuedByUserId { get; set; }
        public int? IssuedByInstructorId { get; set; }
        public DateTime? RevokedAt { get; set; }
        public string RevokeReason { get; set; }
        public string Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public string CourseTitle { get; set; }
        public decimal? CoursePrice { get; set; }
        public DateTime? CourseStartDate { get; set; }
        public DateTime? CourseEndDate { get; set; }
        public int? CourseInstructorId { get; set; }
        public string LocationName { get; set; }
        public string StudentName { get; set; }
        public string StudentPhone { get; set; }
        public string StudentNationalCode { get; set; }
        public string InstructorName { get; set; }

        public bool IsIssued => string.Equals(Status, AcademyConstants.CertificateStatus.Issued, StringComparison.OrdinalIgnoreCase);
    }
}
