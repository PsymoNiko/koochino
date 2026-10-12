using System;

namespace MyShop.Core.Models.Academy
{
    public class AcademyPaymentDto
    {
        public int Id { get; set; }
        public int EnrollmentId { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; }
        public string Source { get; set; }
        public string Gateway { get; set; }
        public string Authority { get; set; }
        public string GatewayRef { get; set; }
        public DateTime? PaidAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int CourseId { get; set; }
        public string CourseTitle { get; set; }
        public int StudentId { get; set; }
        public string StudentName { get; set; }
        public string StudentPhone { get; set; }
        public string EnrollmentStatus { get; set; }
    }
}
