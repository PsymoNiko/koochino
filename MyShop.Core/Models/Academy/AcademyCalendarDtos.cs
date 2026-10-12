using System;

namespace MyShop.Core.Models.Academy
{
    public class AcademyCalendarEventDto
    {
        public string Kind { get; set; }
        public int SessionId { get; set; }
        public int ParentId { get; set; }
        public string ParentTitle { get; set; }
        public string Title { get; set; }
        public DateTime StartsAt { get; set; }
        public DateTime? EndsAt { get; set; }
        public bool HasStartTime { get; set; }
        public int? LocationId { get; set; }
        public string LocationName { get; set; }
        public int? InstructorId { get; set; }
        public string InstructorName { get; set; }
        public string Status { get; set; }
        public int? SessionNumber { get; set; }
        public string RelationStatus { get; set; }
        public int? Capacity { get; set; }
        public int? ApprovedCount { get; set; }
        public int? PendingCount { get; set; }

        public bool IsCourse => string.Equals(Kind, AcademyConstants.AttendanceSessionType.Course, StringComparison.OrdinalIgnoreCase);
        public bool IsCancelled => string.Equals(Status, AcademyConstants.SessionStatus.Cancelled, StringComparison.OrdinalIgnoreCase);
    }

    public class AcademyCalendarRange
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }
    }
}
