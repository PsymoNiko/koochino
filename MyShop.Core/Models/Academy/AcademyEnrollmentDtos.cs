using System;

namespace MyShop.Core.Models.Academy
{
    public class AcademyEnrollmentDto
    {
        public int Id { get; set; }
        public int CourseId { get; set; }
        public int StudentId { get; set; }
        public string Status { get; set; }
        public string StudentNote { get; set; }
        public string ReviewerNote { get; set; }
        public int? ReviewedByInstructorId { get; set; }
        public int? ReviewedByAdminUserId { get; set; }
        public DateTime EnrolledAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public string CourseTitle { get; set; }
        public int? CourseCapacity { get; set; }
        public decimal? CoursePrice { get; set; }
        public int? CourseInstructorId { get; set; }
        public string StudentName { get; set; }
        public string StudentPhone { get; set; }
        public int ApprovedCount { get; set; }
        public int WaitlistedCount { get; set; }
        public int RegisteredCount { get; set; }
        public int? PaymentId { get; set; }
        public string PaymentStatus { get; set; }
        public DateTime? PaidAt { get; set; }
        public string PaymentGateway { get; set; }

        public bool NeedsTuitionPayment =>
            Status == AcademyConstants.EnrollmentStatus.Approved
            && CoursePrice.GetValueOrDefault() > 0
            && !string.Equals(PaymentStatus, AcademyConstants.PaymentStatus.Paid, StringComparison.OrdinalIgnoreCase);
    }

    public class CreateAcademyEnrollmentDto
    {
        public int CourseId { get; set; }
        public int StudentId { get; set; }
        public string StudentNote { get; set; }
    }

    public class ReviewAcademyEnrollmentDto
    {
        public int Id { get; set; }
        public string Status { get; set; }
        public string ReviewerNote { get; set; }
        public int? ReviewedByInstructorId { get; set; }
        public int? ReviewedByAdminUserId { get; set; }
    }
}
