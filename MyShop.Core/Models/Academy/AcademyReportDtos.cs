using System;

namespace MyShop.Core.Models.Academy
{
    public class AcademyAdminSummaryDto
    {
        public int StudentsCount { get; set; }
        public int InstructorsCount { get; set; }
        public int ActiveCourses { get; set; }
        public int PendingEnrollments { get; set; }
        public int WaitlistedEnrollments { get; set; }
        public int ApprovedEnrollments { get; set; }
        public int CompletedEnrollments { get; set; }
        public int PendingActivityRequests { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal OutstandingAmount { get; set; }
        public int SmsOk { get; set; }
        public int SmsFail { get; set; }
    }

    public class AcademyCourseReportDto
    {
        public int CourseId { get; set; }
        public string Title { get; set; }
        public bool IsActive { get; set; }
        public int? Capacity { get; set; }
        public decimal? Price { get; set; }
        public string InstructorName { get; set; }
        public int? InstructorId { get; set; }
        public int RegisteredCount { get; set; }
        public int WaitlistedCount { get; set; }
        public int ApprovedCount { get; set; }
        public int RejectedCount { get; set; }
        public int CancelledCount { get; set; }
        public int CompletedCount { get; set; }
        public int SessionCount { get; set; }
        public int PaidCount { get; set; }
        public int OutstandingCount { get; set; }
        public int PresentCount { get; set; }
        public int AbsentCount { get; set; }
    }

    public class AcademyTuitionReportDto
    {
        public int CourseId { get; set; }
        public string Title { get; set; }
        public decimal Price { get; set; }
        public int ApprovedCount { get; set; }
        public int PaidCount { get; set; }
        public decimal PaidAmount { get; set; }
        public int OutstandingCount { get; set; }
        public decimal OutstandingAmount { get; set; }
    }

    public class AcademyAttendanceReportDto
    {
        public int CourseId { get; set; }
        public string Title { get; set; }
        public int SessionCount { get; set; }
        public int MarkCount { get; set; }
        public int PresentCount { get; set; }
        public int AbsentCount { get; set; }
        public int LateCount { get; set; }
        public int ExcusedCount { get; set; }
        public int PresentPercent => MarkCount <= 0 ? 0 : (int)Math.Round(100.0 * PresentCount / MarkCount);
    }

    public class AcademyActivityReportDto
    {
        public int ActivityId { get; set; }
        public string Title { get; set; }
        public int SessionCount { get; set; }
        public int PendingCount { get; set; }
        public int ApprovedCount { get; set; }
        public int RejectedCount { get; set; }
    }

    public class AcademyInstructorReportDto
    {
        public int InstructorId { get; set; }
        public string InstructorName { get; set; }
        public bool IsActive { get; set; }
        public int CourseCount { get; set; }
        public int ApprovedEnrollments { get; set; }
        public int ActivitySessionCount { get; set; }
        public int PendingRequestCount { get; set; }
    }

    public class AcademyInstructorSummaryDto
    {
        public int ActiveCourses { get; set; }
        public int PendingEnrollments { get; set; }
        public int ApprovedEnrollments { get; set; }
        public int PendingActivityRequests { get; set; }
        public int CourseSessionCount { get; set; }
    }

    public class AcademyStudentSummaryDto
    {
        public int EnrollmentCount { get; set; }
        public int ApprovedCount { get; set; }
        public int WaitlistedCount { get; set; }
        public int UnpaidCount { get; set; }
        public int PresentCount { get; set; }
        public int AbsentCount { get; set; }
        public int PendingActivityRequests { get; set; }
        public int ApprovedActivityRequests { get; set; }
    }

    public class AcademyDailyReportSummaryDto
    {
        public int PendingRequests { get; set; }
        public int WaitlistedRequests { get; set; }
        public int ApprovedRequests { get; set; }
        public int RejectedRequests { get; set; }
        public int PresentCount { get; set; }
        public int AbsentCount { get; set; }
        public int LateCount { get; set; }
        public int ExcusedCount { get; set; }
    }

    public class AcademyDailyByDateReportDto
    {
        public int CourseId { get; set; }
        public string CourseTitle { get; set; }
        public int ScheduleId { get; set; }
        public DateTime SessionDate { get; set; }
        public int? Capacity { get; set; }
        public int PendingCount { get; set; }
        public int WaitlistedCount { get; set; }
        public int ApprovedCount { get; set; }
        public int RejectedCount { get; set; }
        public int PresentCount { get; set; }
        public int AbsentCount { get; set; }
        public int LateCount { get; set; }
        public int ExcusedCount { get; set; }
        public bool IsCancelled { get; set; }
    }

    public class AcademyDailyByCourseReportDto
    {
        public int CourseId { get; set; }
        public string Title { get; set; }
        public int PendingCount { get; set; }
        public int WaitlistedCount { get; set; }
        public int ApprovedCount { get; set; }
        public int RejectedCount { get; set; }
        public int PresentCount { get; set; }
        public int AbsentCount { get; set; }
    }
}
