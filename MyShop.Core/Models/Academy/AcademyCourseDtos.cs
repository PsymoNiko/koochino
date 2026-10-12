using System;
using System.Collections.Generic;

namespace MyShop.Core.Models.Academy
{
    public class AcademyCourseDto
    {
        public int Id { get; set; }
        public int? SiteId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int? LocationId { get; set; }
        public string LocationName { get; set; }
        public int? InstructorId { get; set; }
        public string InstructorName { get; set; }
        public decimal? Price { get; set; }
        public int? Capacity { get; set; }
        public int? NumberOfSessions { get; set; }
        public DateTime? RegistrationStart { get; set; }
        public DateTime? RegistrationEnd { get; set; }
        public DateTime? CourseStartDate { get; set; }
        public DateTime? CourseEndDate { get; set; }
        public string Requirements { get; set; }
        public string ThingsToBring { get; set; }
        public string ImageUrl { get; set; }
        public bool IsActive { get; set; }
        public int SortOrder { get; set; }
        public int GeneratedSessionCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<AcademyCourseScheduleDto> Schedules { get; set; } = new();
    }

    public class CreateAcademyCourseDto
    {
        public int? SiteId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int? LocationId { get; set; }
        public int? InstructorId { get; set; }
        public decimal? Price { get; set; }
        public int? Capacity { get; set; }
        public int? NumberOfSessions { get; set; }
        public DateTime? RegistrationStart { get; set; }
        public DateTime? RegistrationEnd { get; set; }
        public DateTime? CourseStartDate { get; set; }
        public DateTime? CourseEndDate { get; set; }
        public string Requirements { get; set; }
        public string ThingsToBring { get; set; }
        public string ImageUrl { get; set; }
        public bool IsActive { get; set; } = true;
        public int SortOrder { get; set; }
        public List<AcademyCourseScheduleDto> Schedules { get; set; } = new();
    }

    public class UpdateAcademyCourseDto : CreateAcademyCourseDto
    {
        public int Id { get; set; }
    }

    public class AcademyCourseScheduleDto
    {
        public int Id { get; set; }
        public int CourseId { get; set; }
        public int DayId { get; set; }
        public string DayName { get; set; }
        public int Hour { get; set; }
        public int Minute { get; set; }
        public int? DurationMinutes { get; set; }
        public int SortOrder { get; set; }
        /// <summary>Weekly (پیش‌فرض) یا Daily. مقدار خالی در خواندن یعنی Weekly.</summary>
        public string RecurrenceType { get; set; } = AcademyConstants.ScheduleRecurrenceType.Weekly;
        /// <summary>فقط Daily — DATE میلادی در دیتابیس.</summary>
        public DateTime? StartDate { get; set; }
        /// <summary>فقط Daily — DATE میلادی در دیتابیس.</summary>
        public DateTime? EndDate { get; set; }
        /// <summary>ظرفیت هر تاریخ Daily. تهی یعنی نامحدود. مستقل از Capacity ثبت‌نام دوره.</summary>
        public int? Capacity { get; set; }

        public bool IsDaily =>
            string.Equals(RecurrenceType, AcademyConstants.ScheduleRecurrenceType.Daily, StringComparison.OrdinalIgnoreCase);

        /// <summary>Weekly صریح یا مقدار خالی (سازگاری ردیف‌های قدیمی). نوع ناشناخته Weekly محسوب نمی‌شود.</summary>
        public bool IsWeekly =>
            string.IsNullOrWhiteSpace(RecurrenceType)
            || string.Equals(RecurrenceType, AcademyConstants.ScheduleRecurrenceType.Weekly, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>جلسه منطقی Daily روی ScheduleId + SessionDate — بدون ردیف AcademyCourseSessions.</summary>
    public class AcademyLogicalSessionDto
    {
        public int ScheduleId { get; set; }
        public int CourseId { get; set; }
        public DateTime SessionDate { get; set; }
        public int Hour { get; set; }
        public int Minute { get; set; }
        public int DurationMinutes { get; set; }
        public string RecurrenceType { get; set; } = AcademyConstants.ScheduleRecurrenceType.Daily;
        public int? Capacity { get; set; }
        public int ApprovedCount { get; set; }
        public int? InstructorId { get; set; }
        public bool HasException { get; set; }
        public bool IsCancelled { get; set; }
        public string ExceptionNote { get; set; }
        public string CourseTitle { get; set; }
        public int PendingCount { get; set; }
        public int WaitlistedCount { get; set; }
        /// <summary>وضعیت درخواست دانش‌آموز جاری برای همین روز منطقی؛ تهی یعنی هنوز درخواست نداده.</summary>
        public string MyRequestStatus { get; set; }
        public string MyReviewerNote { get; set; }
        public bool IsFull => Capacity is > 0 && ApprovedCount >= Capacity.Value;
    }

    public class AcademyCourseSessionDto
    {
        public int Id { get; set; }
        public int CourseId { get; set; }
        public int SessionNumber { get; set; }
        public DateTime SessionDate { get; set; }
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
        public int? LocationId { get; set; }
        public string LocationName { get; set; }
        public string Title { get; set; }
        public string Status { get; set; }
        public string Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class UpsertAcademyCourseSessionDto
    {
        public int? Id { get; set; }
        public int CourseId { get; set; }
        public int SessionNumber { get; set; }
        public DateTime SessionDate { get; set; }
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
        public int? LocationId { get; set; }
        public string Title { get; set; }
        public string Status { get; set; } = AcademyConstants.SessionStatus.Scheduled;
        public string Notes { get; set; }
    }
}
