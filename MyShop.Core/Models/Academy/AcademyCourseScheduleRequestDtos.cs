using System;

namespace MyShop.Core.Models.Academy
{
    public class AcademyCourseScheduleRequestDto
    {
        public int Id { get; set; }
        public int ScheduleId { get; set; }
        public int StudentId { get; set; }
        public DateTime SessionDate { get; set; }
        public string Status { get; set; } = AcademyConstants.RequestStatus.Pending;
        public string StudentNote { get; set; }
        public string ReviewerNote { get; set; }
        public int? ReviewedByInstructorId { get; set; }
        public int? ReviewedByAdminUserId { get; set; }
        public DateTime RequestedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int CourseId { get; set; }
        public int? CourseInstructorId { get; set; }
        public string CourseTitle { get; set; }
        public int Hour { get; set; }
        public int Minute { get; set; }
        public int? DurationMinutes { get; set; }
        public string StudentName { get; set; }
    }

    public class CreateAcademyCourseScheduleRequestDto
    {
        public int ScheduleId { get; set; }
        public int StudentId { get; set; }
        public DateTime SessionDate { get; set; }
        public string StudentNote { get; set; }
    }

    public class ReviewAcademyCourseScheduleRequestDto
    {
        public int Id { get; set; }
        public string Status { get; set; }
        public string ReviewerNote { get; set; }
        public int? ReviewedByInstructorId { get; set; }
        public int? ReviewedByAdminUserId { get; set; }
    }

    public class AcademyCourseScheduleRequestAuditDto
    {
        public int Id { get; set; }
        public int RequestId { get; set; }
        public string OldStatus { get; set; }
        public string NewStatus { get; set; }
        public string ReviewerNote { get; set; }
        public int? ReviewedByInstructorId { get; set; }
        public int? ReviewedByAdminUserId { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class AcademyDailyApprovedCountDto
    {
        public int ScheduleId { get; set; }
        public DateTime SessionDate { get; set; }
        public int ApprovedCount { get; set; }
    }
}
