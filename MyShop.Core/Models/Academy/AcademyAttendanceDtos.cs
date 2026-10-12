using System;
using System.Collections.Generic;

namespace MyShop.Core.Models.Academy
{
    public class AcademyAttendanceDto
    {
        public int Id { get; set; }
        public int? CourseSessionId { get; set; }
        public int? ActivitySessionId { get; set; }
        public int? ScheduleId { get; set; }
        public DateTime? SessionDate { get; set; }
        public int StudentId { get; set; }
        public string Status { get; set; }
        public string Source { get; set; }
        public string Notes { get; set; }
        public int? MarkedByInstructorId { get; set; }
        public int? MarkedByAdminUserId { get; set; }
        public DateTime MarkedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string StudentName { get; set; }
        public string StudentPhone { get; set; }
        public string SessionTitle { get; set; }
    }

    public class AcademyAttendanceRosterItemDto
    {
        public int? CourseSessionId { get; set; }
        public int? ActivitySessionId { get; set; }
        public int? ScheduleId { get; set; }
        public int? CourseId { get; set; }
        public int? ActivityId { get; set; }
        public string SessionTitle { get; set; }
        public int? SessionNumber { get; set; }
        public DateTime? SessionDate { get; set; }
        public TimeSpan? StartTime { get; set; }
        public int? CourseInstructorId { get; set; }
        public int StudentId { get; set; }
        public string StudentName { get; set; }
        public string StudentPhone { get; set; }
        public int? AttendanceId { get; set; }
        public string Status { get; set; }
        public string Source { get; set; }
        public string Notes { get; set; }
        public DateTime? MarkedAt { get; set; }
        public int? MarkedByInstructorId { get; set; }
    }

    public class UpsertAcademyAttendanceDto
    {
        public int? CourseSessionId { get; set; }
        public int? ActivitySessionId { get; set; }
        public int? ScheduleId { get; set; }
        public DateTime? SessionDate { get; set; }
        public int StudentId { get; set; }
        public string Status { get; set; } = AcademyConstants.AttendanceStatus.Present;
        public string Source { get; set; } = AcademyConstants.AttendanceSource.Manual;
        public string Notes { get; set; }
        public int? MarkedByInstructorId { get; set; }
        public int? MarkedByAdminUserId { get; set; }
    }

    public class AcademyAttendanceQrTokenDto
    {
        public int Id { get; set; }
        public string SessionType { get; set; }
        public int SessionId { get; set; }
        public string Token { get; set; }
        public DateTime ExpiresAt { get; set; }
        public int? CreatedByInstructorId { get; set; }
        public int? CreatedByAdminUserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsRevoked { get; set; }
        public string SessionTitle { get; set; }
        public DateTime? SessionDate { get; set; }
        public int? InstructorId { get; set; }
    }
}
